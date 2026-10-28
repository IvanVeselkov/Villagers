# Mirror: самоучитель и рецепты для мультиплеера

> Версия Mirror в проекте: **96.0.1** (лежит в `Assets/Mirror`, не UPM-пакет).
> Unity: **6000.5.3f1**, URP, Input System, AI Navigation.
> Все примеры ниже проверены по исходникам `Assets/Mirror` этой версии.

Этот файл — не перевод документации, а **практическое руководство**: как думать в терминах Mirror, какие есть варианты реализации одной и той же механики и какой вариант выбирать в каком случае.

---

## Содержание

1. [Десять правил, которые экономят недели](#1-десять-правил-которые-экономят-недели)
2. [Модель Mirror за 10 минут](#2-модель-mirror-за-10-минут)
3. [Чек-лист запуска проекта](#3-чек-лист-запуска-проекта)
4. [Жизненный цикл NetworkBehaviour](#4-жизненный-цикл-networkbehaviour)
5. [Синхронизация состояния: SyncVar](#5-синхронизация-состояния-syncvar)
6. [Коллекции: SyncList, SyncDictionary, SyncSet](#6-коллекции-synclist-syncdictionary-syncset)
7. [Команды и RPC](#7-команды-и-rpc)
8. [Спавн объектов](#8-спавн-объектов)
9. [Движение и трансформ](#9-движение-и-трансформ)
10. [Анимация](#10-анимация)
11. [Рецепты механик](#11-рецепты-механик)
    - [11.1 Здоровье и HP-слайдер](#111-здоровье-и-hp-слайдер)
    - [11.2 Атака и урон](#112-атака-и-урон)
    - [11.3 Подбор, выброс, инвентарь](#113-подбор-выброс-инвентарь)
    - [11.4 NPC и AI](#114-npc-и-ai)
    - [11.5 Чат](#115-чат)
    - [11.6 Лобби и комнаты](#116-лобби-и-комнаты)
    - [11.7 Выбор персонажа и точки спавна](#117-выбор-персонажа-и-точки-спавна)
    - [11.8 Взаимодействие и подсказки UI](#118-взаимодействие-и-подсказки-ui)
    - [11.9 Смерть и респавн](#119-смерть-и-респавн)
12. [UI в мультиплеере](#12-ui-в-мультиплеере)
13. [Безопасность](#13-безопасность)
14. [Производительность](#14-производительность)
15. [Отладка и типовые ошибки](#15-отладка-и-типовые-ошибки)
16. [Карта проекта и план миграции](#16-карта-проекта-и-план-миграции)
17. [Шпаргалка](#17-шпаргалка)
18. [Ссылки и что читать дальше](#18-ссылки-и-что-читать-дальше)

---

## 1. Десять правил, которые экономят недели

1. **Сервер — единственный источник правды.** Клиент присылает *намерение* («хочу ударить», «хочу подобрать»), сервер проверяет и меняет состояние.
2. **Клиент не создаёт и не уничтожает сетевые объекты.** Только `NetworkServer.Spawn` / `NetworkServer.Destroy` на сервере.
3. **`SyncVar` — для состояния, `Command` — для действий.** Не пытайтесь «синхронизировать действие» через SyncVar.
4. **`[Command]` по умолчанию требует authority.** На объекте игрока authority есть у его владельца. На объекте мира — нет, пока не выдали через `AssignClientAuthority`.
5. **Hook у `SyncVar` вызывается у клиентов, но не на выделенном сервере.** В host-режиме hook срабатывает, потому что host — тоже клиент. Это частая причина путаницы с UI.
6. **`isLocalPlayer` — только для ввода и локального UI.** Вся игровая логика — по `isServer`.
7. **Всё, что клиент присылает в Command, — ложь, пока не проверено.** Дистанция, кулдаун, наличие предмета, ownership.
8. **Один объект = один `NetworkIdentity`.** `NetworkBehaviour` без `NetworkIdentity` в родителях не работает.
9. **Runtime-объекты должны быть зарегистрированы** в `NetworkManager.spawnPrefabs` (или через `NetworkClient.RegisterPrefab`) — иначе клиент не сможет их заспавнить.
10. **Сначала проверяйте host, потом два клиента.** Host скрывает половину ошибок, потому что сервер и клиент — один процесс.

---

## 2. Модель Mirror за 10 минут

### 2.1 Кто есть кто

```
┌─────────────────────────── SERVER ───────────────────────────┐
│  NetworkManager (singleton)                                  │
│  NetworkServer.spawned: Dictionary<uint netId, NetworkIdentity> │
│  Авторитетная симуляция: физика, урон, инвентарь, AI         │
└───────────────▲───────────────────────────────▲──────────────┘
                │ Command (client → server)     │ SyncVar / ClientRpc
                │                               │ (server → clients)
┌───────────────┴───────────────┐   ┌───────────┴──────────────┐
│ CLIENT A (owner игрока A)     │   │ CLIENT B                 │
│ NetworkClient.spawned         │   │ NetworkClient.spawned    │
│ Ввод, камера, локальный UI    │   │ Только отображение       │
└───────────────────────────────┘   └──────────────────────────┘
```

- **Host** — это сервер и клиент в одном процессе (`NetworkServer.active && NetworkClient.active`). У хоста `isServer == true`, `isClient == true`, а его игрок — `isLocalPlayer == true`.
- **Authority (владение)** — у объекта есть `connectionToClient`. Для player prefab Mirror выдаёт authority владельцу автоматически при `AddPlayerForConnection`.
- **`netId`** — уникальный ID объекта в сети. Живёт от спавна до уничтожения. По нему можно найти объект в `NetworkServer.spawned` / `NetworkClient.spawned`.

### 2.2 Три канала общения

| Что нужно | Инструмент | Направление | Где менять |
|---|---|---|---|
| Постоянное состояние (HP, имя, счёт, слоты) | `[SyncVar]`, `SyncList<T>` | server → clients (по умолчанию) | на сервере |
| Действие клиента | `[Command]` | client → server | вызывается на клиенте, выполняется на сервере |
| Разовое событие всем | `[ClientRpc]` | server → all clients | вызывается на сервере |
| Разовое событие одному | `[TargetRpc]` | server → one client | вызывается на сервере |
| Сообщение вне объекта | `NetworkMessage` | client ↔ server | `NetworkClient.Send` / `NetworkServer.SendToAll` |

### 2.3 Что происходит при подключении

1. Клиент подключается к транспорту (KCP/Telepathy/SimpleWeb).
2. Сервер вызывает `OnServerConnect(conn)`.
3. Клиент готов — сервер вызывает `OnServerAddPlayer(conn)`.
4. По умолчанию `OnServerAddPlayer` делает `Instantiate(playerPrefab)` и `NetworkServer.AddPlayerForConnection(conn, player)`.
5. Сервер рассылает клиентам spawn-сообщения. На клиентах создаются копии объектов и вызываются `OnStartClient`, у владельца — `OnStartAuthority` и `OnStartLocalPlayer`.
6. Дальше — SyncVar-дельты, RPC и трансформ.

> Важно: `OnStartServer` вызывается **до** того, как у объекта появятся наблюдатели. Идеальное место, чтобы наполнить `SyncList` начальными значениями.

### 2.4 Weaver: почему сетевой код «магически» работает

Mirror использует IL-постпроцессор (Weaver). При компиляции он:

- превращает `[SyncVar]` в свойства с dirty-битами;
- генерирует сериализацию и проверки для `[Command]`/`[ClientRpc]`/`[TargetRpc]`;
- создаёт read/write для ваших struct-ов, используемых в сети.

Практические следствия:

- Если в консоли ошибка Weaver — скрипт не скомпилируется, пока не исправите тип.
- `SyncVar` нельзя делать `static`, `readonly` или свойством.
- `SyncList` объявляйте как `readonly`.
- Пользовательские типы для сети — `struct` с public-полями, без ссылок на `GameObject`.

---

## 3. Чек-лист запуска проекта

Это состояние **именно этого проекта** на момент написания. Проверяйте по порядку.

### 3.1 NetworkManager

В сцене `Assets/Scenes/TestScene.unity` есть объект `Netman` с компонентами `NetMan` (наследник `NetworkManager`) и `NetworkManagerHUD`.

| Пункт | Сейчас | Что сделать |
|---|---|---|
| `transport` | ❌ **пусто** (`transport: {fileID: 0}`) | Добавить на `Netman` компонент `KcpTransport` (`Assets/Mirror/Transports/KCP/KcpTransport.cs`) и перетащить его в поле `Transport`. Без транспорта Start Host/Server/Client не работает. |
| `playerPrefab` | ✅ `Assets/Prefabs/PlayerObjects.prefab` | Оставить. |
| `spawnPrefabs` | ❌ пустой список | Добавить туда все префабы, которые спавнятся в рантайме: предметы, стрелы, дроп, NPC-спавны и т.п. |
| `playerSpawnMethod` | `1` (RoundRobin) | Нормально, если в сцене есть `NetworkStartPosition`. Иначе игроки появляются в позиции префаба. |
| `autoCreatePlayer` | ✅ `true` | Оставить, пока нет своего лобби. |
| `NetworkManagerHUD` | ✅ есть | Быстрый способ проверить Host/Client. В релизе убрать. |

### 3.2 Player prefab (`Assets/Prefabs/PlayerObjects.prefab`)

- ✅ `NetworkIdentity`
- ✅ `NetworkTransformReliable` (`syncDirection = ClientToServer`, target — дочерний трансформ)
- ⚠️ `NetworkAnimator`: `clientAuthority = false`, `syncDirection = ClientToServer`. Это значит, что анимации должны идти **с сервера**. Локальный `AnimatorChanger.DoAttack()` на клиенте не будет разослан другим. Варианты:
  - поставить `clientAuthority = true` (анимации игрока идут от владельца) — самый простой путь для player prefab;
  - либо вызывать анимацию через `[Command]` → серверный `Rpc`/`NetworkAnimator.SetTrigger`.
- ❌ камера помечена `Untagged`, поэтому `Camera.main` и поиск по тегу `MainCamera` возвращают `null`. Держите прямую ссылку на камеру в компоненте, а не ищите её каждый кадр.
- ⚠️ `HealthSystem`, `AttackManager`, `CombatBase`, `NetMovement`, `Inventory`, `TakingObject`, `DropObject` — часть из них `MonoBehaviour`, а не `NetworkBehaviour`. Подробности в [разделе 16](#16-карта-проекта-и-план-миграции).

### 3.3 Сцена

- Объекты с `NetworkIdentity`, которые стоят в сцене (NPC, сундуки, стационарные предметы), должны иметь **`sceneId != 0`**. Mirror присваивает его автоматически при сохранении сцены в редакторе. Если `sceneId == 0`, объект не будет заспавнен.
- Проверить: выделить объект → `NetworkIdentity` → поле `Scene Id` должно быть не 0. Если 0 — сохранить сцену (Ctrl+S) или нажать `Rebuild Scene Ids` (если доступно в инспекторе).
- Предметы на земле (`GroundedObject`, `GroundedWeapon`, `GroundedSecondWeapon`) сейчас **без `NetworkIdentity`** — это обычные MonoBehaviours. Пока их не трогать, но при переходе на сетевой инвентарь их придётся либо сделать scene-объектами, либо заменить на спавн из префаба.

### 3.4 Быстрый тест

1. Добавить `KcpTransport` на `Netman`, назначить в `Transport`.
2. Play → `NetworkManagerHUD` → **Host (Server + Client)**.
3. Убедиться, что появился игрок и HUD.
4. Собрать билд (File → Build) и запустить второй экземпляр: в билде указать `localhost` (или IP хоста) → **Client**.
5. Если хост видит клиента, а клиент — хоста, база работает.

> Для теста нескольких клиентов в редакторе удобны **ParrelSync** (сторонний) или Unity **Multiplayer Play Mode** (пакет). Второй билд — самый надёжный вариант.

---

## 4. Жизненный цикл NetworkBehaviour

```csharp
using Mirror;
using UnityEngine;

public class LifecycleDemo : NetworkBehaviour
{
    public override void OnStartServer()
    {
        // Только сервер (и host). Объект уже в NetworkServer.spawned.
        // Здесь можно инициализировать серверные данные.
    }

    public override void OnStopServer() { }

    public override void OnStartClient()
    {
        // На клиенте, когда копия объекта создана.
        // netId уже валиден.
    }

    public override void OnStopClient() { }

    public override void OnStartAuthority()
    {
        // У владельца объекта (для player prefab — у его клиента).
        // Хорошее место, чтобы включить камеру/ввод.
    }

    public override void OnStopAuthority() { }

    public override void OnStartLocalPlayer()
    {
        // Только у локального игрока. Самый узкий и самый безопасный callback для UI/ввода.
    }

    public override void OnStopLocalPlayer() { }
}
```

Порядок для player prefab на клиенте-владельце:

```
OnStartServer (на сервере)
OnStartClient
OnStartAuthority
OnStartLocalPlayer
```

На «чужом» клиенте:

```
OnStartClient
```

### 4.1 isServer / isClient / isOwned / isLocalPlayer

| Свойство | Где true | Для чего использовать |
|---|---|---|
| `isServer` | сервер и host | игровая логика, изменение SyncVar, спавн |
| `isClient` | клиент и host | клиентская логика, UI |
| `isOwned` | у клиента, которому принадлежит объект (и у host для его игрока) | «это мой объект», проверка в Command |
| `isLocalPlayer` | только у player prefab локального игрока | ввод, камера, HUD |
| `authority` | право менять синхронизируемое состояние; на клиенте — `ClientToServer` + `isOwned`, на сервере — `ServerToClient` | проверка перед записью в sync-состояние |
| `isServerOnly` / `isClientOnly` | только сервер / только клиент | отладка |

**Правило:** `isLocalPlayer` — для ввода и UI. Всё, что меняет мир, — под `isServer`.

> Для `[Command]` проверяется `isOwned`/`isLocalPlayer` на клиенте и `identity.connectionToClient == conn` на сервере, а не свойство `authority`. `authority` относится к праву менять синхронизируемое состояние компонента.

### 4.2 Кэширование ссылок

`GetComponent` в `Update` — плохо. Кэшируйте в `Awake`/`OnStartClient`. Но помните: `Awake` вызывается до того, как Mirror присвоит `netId`, поэтому сетевые вызовы в `Awake` делать нельзя.

```csharp
HealthSystem health;

void Awake() => health = GetComponent<HealthSystem>();

public override void OnStartClient()
{
    // здесь уже можно безопасно читать SyncVar и подписываться на события
}
```

---

## 5. Синхронизация состояния: SyncVar

### 5.1 Базовый вариант

```csharp
using Mirror;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score;

    void OnScoreChanged(int oldValue, int newValue)
    {
        // Вызывается на клиентах при получении нового значения.
        // НЕ вызывается на сервере/host, который сам изменил значение.
        Debug.Log($"Score: {oldValue} -> {newValue}");
    }

    [Server]
    public void AddScore(int amount)
    {
        score += amount; // менять только на сервере
    }
}
```

Правила:

- Менять `SyncVar` можно **только на сервере** (если `syncDirection = ServerToClient`, по умолчанию).
- Hook имеет сигнатуру `void Hook(T oldValue, T newValue)`.
- Hook **вызывается у клиентов (включая host), но не на выделенном сервере (server-only)**. Если UI нужен и на сервере — обновляйте его ещё и вручную в методе-мутаторе.
- `SyncVar` не поддерживает `List<T>`, `Dictionary<K,V>`, обычные классы. Для коллекций — `SyncList`/`SyncDictionary`; для составных данных — struct с public-полями.
- Не кладите в `SyncVar` секреты: все клиенты их видят.

### 5.2 SyncVar с составным struct

```csharp
[System.Serializable]
public struct PlayerStats
{
    public float health;
    public float mana;
    public int level;
}

public class PlayerStatsNet : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnStatsChanged))]
    public PlayerStats stats;

    void OnStatsChanged(PlayerStats oldStats, PlayerStats newStats)
    {
        // обновить UI
    }

    [Server]
    public void SetHealth(float value)
    {
        PlayerStats s = stats; // struct — копия
        s.health = value;
        stats = s;             // присваивание целиком = dirty
    }
}
```

> Важно: изменение поля внутри struct-а `stats.health = 5` **не** помечает SyncVar грязным. Нужно присвоить struct целиком.

### 5.3 Альтернатива: свойство + SyncVar

```csharp
[SyncVar] float _health = 100f;
public float Health => _health;

[Server]
public void SetHealth(float value) => _health = Mathf.Clamp(value, 0f, 100f);
```

Плюс: снаружи нельзя случайно изменить. Минус: больше кода. Для игровых систем лучше так.

### 5.4 SyncVar vs RPC: что выбрать

| Ситуация | Выбор |
|---|---|
| HP, счёт, имя, состояние двери | `SyncVar` |
| Список предметов, игроков в лобби | `SyncList<T>` |
| «Проиграть звук выстрела» | `ClientRpc` |
| «Показать ошибку конкретному клиенту» | `TargetRpc` |
| «Я нажал кнопку атаки» | `Command` |
| «Каждое движение» | `NetworkTransform`, не SyncVar |

---

## 6. Коллекции: SyncList, SyncDictionary, SyncSet

`SyncList<T>` — основной инструмент для инвентаря, лобби, списка игроков. Это не `List<T>`: изменения реплицируются дельтами.

```csharp
using Mirror;
using UnityEngine;

[System.Serializable]
public struct ItemStack
{
    public int itemId;
    public int count;
}

public class PlayerInventoryNet : NetworkBehaviour
{
    public const int MaxSlots = 8;

    // readonly обязателен: ссылка не должна пересоздаваться
    public readonly SyncList<ItemStack> slots = new SyncList<ItemStack>();

    public override void OnStartServer()
    {
        // начальное состояние. Клиенты получат его при спавне.
        for (int i = 0; i < MaxSlots; i++)
            slots.Add(default);
    }

    public override void OnStartClient()
    {
        // можно подписаться на изменения для UI
        slots.OnSet += (index, item) => RefreshSlot(index);
        slots.OnClear += RefreshAll;
    }

    void RefreshSlot(int index) { /* обновить UI */ }
    void RefreshAll() { /* обновить весь инвентарь */ }
}
```

> Начальное состояние `SyncList` приходит **до** `OnStartClient`, и события `OnAdd`/`OnSet` для него не вызываются (Mirror заполняет список напрямую в `OnDeserializeAll`). Отрисуйте стартовое состояние вручную в `OnStartClient`.

### 6.1 События SyncList (проверено в 96.0.1)

| Событие | Сигнатура | Когда |
|---|---|---|
| `OnAdd` | `Action<int>` (index) | добавлен элемент |
| `OnInsert` | `Action<int>` | вставка |
| `OnSet` | `Action<int,T>` | замена по индексу |
| `OnRemove` | `Action<int,T>` | удаление |
| `OnClear` | `Action` | очистка |
| `OnChange` | `Action<Operation,int,T>` | любое изменение |
| `Callback` | `Action<Operation,int,T,T>` | полный вариант: операция, индекс, старое, новое |

`Operation` — enum `OP_ADD`, `OP_SET`, `OP_INSERT`, `OP_REMOVEAT`, `OP_CLEAR`.

> В `OnChange` для `OP_SET` и `OP_REMOVEAT` третьим параметром приходит **старое** значение, а для `OP_ADD`/`OP_INSERT` — новое. Если нужны оба — используйте `Callback`.

### 6.2 Правила работы с SyncList

- Менять **только на сервере** (если не включили `syncDirection = ClientToServer`).
- Не заменять ссылку: `slots = new SyncList<...>()` — нельзя. Только `Clear/Add/RemoveAt/Insert`.
- Порядок элементов одинаков на сервере и клиентах. Индекс — стабильный идентификатор слота.
- Не храните в элементах ссылки на `GameObject`/`NetworkBehaviour`. Храните `netId` (`uint`) или числовые ID.
- Для «сетов» (уникальные предметы) есть `SyncSet<T>`, для словарей — `SyncDictionary<K,V>`.
- `SyncList` шлёт дельту. Если меняете 100 элементов за кадр — лучше сделать один «снимок» через `Clear` + `AddRange` или пересмотреть архитектуру.

### 6.3 SyncDictionary

```csharp
public readonly SyncDictionary<int, int> ammoByItemId = new SyncDictionary<int, int>();

[Server]
public void SetAmmo(int itemId, int ammo)
{
    if (ammo <= 0) ammoByItemId.Remove(itemId);
    else ammoByItemId[itemId] = ammo;
}
```

### 6.4 Ограничение: вложенные коллекции

`SyncList<SyncList<int>>` не поддерживается. Если нужна вложенность — плоская структура с `ownerId`/`slotIndex` или отдельные `NetworkBehaviour` на каждый контейнер.

---

## 7. Команды и RPC

### 7.1 Command: клиент → сервер

```csharp
public class PlayerInteract : NetworkBehaviour
{
    public Transform cameraTransform;
    public float interactRange = 3f;
    public LayerMask interactMask;

    [Command]
    void CmdInteract(uint targetNetId)
    {
        // Выполняется на сервере.
        // Все аргументы пришли от клиента — им нельзя доверять.
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity target))
            return;

        if (Vector3.Distance(transform.position, target.transform.position) > interactRange + 1f)
            return;

        // ... серверная логика
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
            return;

        NetworkIdentity target = hit.collider.GetComponentInParent<NetworkIdentity>();
        if (target != null)
            CmdInteract(target.netId);
    }
}
```

Ключевые моменты:

- `[Command]` по умолчанию `requiresAuthority = true`: клиент должен владеть объектом, на котором вызван Command. Для player prefab это так.
- Если Command на объекте мира (дверь, сундук), у клиента нет authority. Варианты:
  1. Вызывать Command на **своём игроке**, а объект находить по `netId` (рекомендуется);
  2. Выдать authority: `identity.AssignClientAuthority(conn)` (подходит для «предмета в руках»);
  3. `[Command(requiresAuthority = false)]` — только если действительно понимаете последствия.
- Аргументы должны быть сериализуемыми Mirror: примитивы, struct с public-полями, `NetworkIdentity`, `GameObject` (осторожно), массивы.
- Command можно вызывать только на **заспавненном** объекте. Иначе в консоли «Command called on unspawned object».
- Не делайте Command «на каждый кадр» — это RPC-трафик. Для движения есть `NetworkTransform`.

### 7.2 ClientRpc: сервер → все клиенты

```csharp
[ClientRpc]
void RpcPlayHitEffect(Vector3 position, int damage)
{
    // Выполняется на всех клиентах, включая владельца (includeOwner = true по умолчанию).
    Instantiate(hitEffectPrefab, position, Quaternion.identity);
}

[Server]
void ServerHit(Vector3 position, int damage)
{
    RpcPlayHitEffect(position, damage);
}
```

- `[ClientRpc(includeOwner = false)]` — если владельцу эффект не нужен (он уже проиграл локально).
- Не передавайте в RPC `GameObject`/компоненты. Передавайте `netId`, позиции, числа.
- RPC можно вызывать только после спавна объекта. Если объект уничтожается в том же кадре — сначала RPC, потом `Destroy`.
- Для «частых» эффектов (каждый выстрел дробовика) RPC дешевле, чем SyncVar.

### 7.3 TargetRpc: сервер → один клиент

```csharp
[TargetRpc]
void TargetShowError(NetworkConnectionToClient target, string message)
{
    // Выполняется только у указанного клиента.
    ui.ShowError(message);
}

[Server]
void ServerReject(NetworkConnectionToClient conn)
{
    TargetShowError(conn, "Недостаточно места в инвентаре");
}
```

Первый параметр — `NetworkConnectionToClient` (или `NetworkConnection`). Он не передаётся по сети как данные, Mirror подставляет адресата.

### 7.4 NetworkMessage: сообщения вне объектов

Когда нет подходящего `NetworkBehaviour` (логин, лобби, чат до спавна игрока):

```csharp
using Mirror;
using UnityEngine;

public struct ChatSendMessage : NetworkMessage
{
    public string text;
}

public struct ChatRelayMessage : NetworkMessage
{
    public int senderConnectionId;
    public string text;
}

public class ChatNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<ChatSendMessage>(OnChatSend);
    }

    void OnChatSend(NetworkConnectionToClient conn, ChatSendMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.text)) return;
        if (msg.text.Length > 200) return;

        NetworkServer.SendToAll(new ChatRelayMessage
        {
            senderConnectionId = conn.connectionId,
            text = msg.text
        });
    }
}
```

Клиент:

```csharp
public class ChatClient : MonoBehaviour
{
    void OnEnable()  => NetworkClient.RegisterHandler<ChatRelayMessage>(OnChatRelay);
    void OnDisable() => NetworkClient.UnregisterHandler<ChatRelayMessage>();

    void OnChatRelay(ChatRelayMessage msg)
    {
        Debug.Log($"[{msg.senderConnectionId}]: {msg.text}");
    }

    public void Send(string text)
    {
        if (!NetworkClient.isConnected) return;
        NetworkClient.Send(new ChatSendMessage { text = text });
    }
}
```

> `NetworkClient.RegisterHandler` нужно вызывать **до** подключения (или сразу после), иначе можно пропустить первые сообщения. `UnregisterHandler` есть в `NetworkClient`.

### 7.5 Валидация Command: обязательный минимум

```csharp
[Command]
void CmdUseItem(int slotIndex)
{
    // 1. Индексы
    if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

    // 2. Состояние
    if (!IsAlive()) return;

    // 3. Кулдаун (серверный!)
    if (Time.time < nextUseTime) return;
    nextUseTime = Time.time + 0.5f;

    // 4. Дистанция до цели (если есть цель)
    // 5. Наличие предмета
    // 6. Только потом — действие
}
```

---

## 8. Спавн объектов

### 8.1 Три категории объектов

| Категория | Как появляется | Нужен `spawnPrefabs`? | Нужен `sceneId`? |
|---|---|---|---|
| Player | `NetworkServer.AddPlayerForConnection` | Нет (есть `playerPrefab`) | Нет |
| Scene object | уже лежит в сцене, сервер вызывает `SpawnObjects()` | Нет | **Да** |
| Runtime object | `Instantiate` + `NetworkServer.Spawn` | **Да** | Нет |

### 8.2 Scene objects

1. Добавить `NetworkIdentity` на объект.
2. Сохранить сцену — Mirror присвоит `sceneId`.
3. Сервер при загрузке сцены автоматически спавнит такие объекты (`NetworkServer.SpawnObjects()`).
4. Клиент при загрузке сцены находит их по `sceneId`.

Mirror при `SpawnObjects()` сам включает scene-объекты с `NetworkIdentity` (в редакторе они хранятся выключенными после post-process). Если объект должен быть «выключен» логически — выключайте компоненты/поведение, а не рассчитывайте, что `SetActive(false)` оставит его вне сети. Для «только на сервере» есть `NetworkIdentity.serverOnly`.

### 8.3 Runtime spawn

```csharp
public class Spawner : NetworkBehaviour
{
    public GameObject worldItemPrefab; // обязательно добавлен в spawnPrefabs

    [Server]
    public void SpawnItem(int itemId, Vector3 position)
    {
        GameObject go = Instantiate(worldItemPrefab, position, Quaternion.identity);
        WorldItem item = go.GetComponent<WorldItem>();
        item.itemId = itemId;
        NetworkServer.Spawn(go); // без owner — объект принадлежит серверу
    }
}
```

С owner (например, «предмет в руках игрока», стрела игрока):

```csharp
[Server]
public void SpawnOwned(GameObject prefab, NetworkConnectionToClient owner)
{
    GameObject go = Instantiate(prefab);
    NetworkServer.Spawn(go, owner); // authority у клиента
}
```

> `NetworkServer.Spawn(go, owner)` выдаёт authority. Это не значит, что клиент может менять SyncVar — только вызывать Command на этом объекте (если `requiresAuthority`).

### 8.4 Уничтожение

```csharp
[Server]
public void Remove(GameObject go)
{
    NetworkServer.Destroy(go); // UnSpawn + Destroy на всех клиентах
}
```

- `NetworkServer.UnSpawn(go)` — убрать из сети, но оставить объект на сервере (редко нужно).
- **Никогда** не вызывайте `Destroy(go)` на клиенте для сетевого объекта — сервер всё равно пришлёт его обратно или возникнет рассинхрон.
- Если объект уничтожается на сервере, все клиентские копии уничтожаются автоматически.

### 8.5 Динамическая регистрация префабов

Если префабы лежат в `Resources` или загружаются из Addressables и не могут быть в `spawnPrefabs`, их нужно зарегистрировать на клиентах **до** спавна:

```csharp
// На клиенте, после подключения и до спавна объекта:
NetworkClient.RegisterPrefab(prefab);
```

`NetworkIdentity.assetId` у обычного префаба уже вычисляется из GUID префаба в редакторе, поэтому на сервере достаточно `NetworkServer.Spawn(go)`. Если `assetId` нет (префаб создан в рантайме), задайте его одинаково с двух сторон:

```csharp
// Клиент
NetworkClient.RegisterPrefab(prefab, assetId);

// Сервер
GameObject go = Instantiate(prefab);
NetworkServer.Spawn(go, assetId, ownerConnection);
```

Для полностью динамических ассетов есть `NetworkClient.RegisterSpawnHandler(assetId, spawnHandler, unspawnHandler)`. Это продвинутый сценарий; в 99% случаев достаточно `spawnPrefabs`.

### 8.6 Спавн через пул

```csharp
[Server]
public void SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
{
    GameObject go = Pool.Get(prefab);
    go.transform.SetPositionAndRotation(pos, rot);
    NetworkServer.Spawn(go);
}
```

При уничтожении возвращайте объект в пул своей обёрткой над `NetworkServer.Destroy` (сначала `UnSpawn`, затем вернуть в пул и выключить объект), либо используйте простой пул на уровне префаба и `NetworkServer.Destroy`. Публичных `OnDestroyed`-событий у `NetworkServer` в 96 нет — не рассчитывайте на них.

---

## 9. Движение и трансформ

### 9.1 Компоненты

| Компонент | Когда использовать | Плюсы | Минусы |
|---|---|---|---|
| `NetworkTransformReliable` | обычные объекты, двери, платформы | просто, надёжно | больше трафика, задержка |
| `NetworkTransformUnreliable` | быстрое движение, снаряды | меньше задержки | может терять пакеты |
| `NetworkTransformHybrid` | компромисс | меньше трафика | сложнее |
| свой SyncVar + интерполяция | специфические случаи | полный контроль | писать самому |

Поля `NetworkTransformBase`, которые реально влияют:

- `syncDirection`: `ServerToClient` (сервер двигает) или `ClientToServer` (владелец двигает).
- `syncPosition`, `syncRotation`, `syncScale`.
- `onlySyncOnChange` — не слать, если не изменилось.
- `interpolatePosition`, `interpolateRotation` — сглаживание.
- `coordinateSpace`: `Local` или `World`.
- `positionPrecision` (Reliable) — 0.01 = 1 см.
- `syncInterval` (в `NetworkBehaviour`) — как часто слать.

### 9.2 Три архитектуры движения

#### Вариант A. Client authority (сейчас в проекте)

Владелец двигает `CharacterController`, `NetworkTransformReliable` с `ClientToServer` рассылает позицию.

```csharp
void Update()
{
    if (!isOwned) return;          // только владелец
    // Input + CharacterController.Move
}
```

Плюсы: мгновенный отклик, просто.
Минусы: читер может телепортироваться; сервер должен хотя бы проверять скорость.

**Минимальная серверная проверка:**

```csharp
// На сервере, раз в 0.5 с
Vector3 lastPos;
float lastCheck;

[ServerCallback]
void CheckSpeed()
{
    if (Time.time - lastCheck < 0.5f) return;
    float distance = Vector3.Distance(transform.position, lastPos);
    if (distance > maxSpeed * 0.5f + 1f)
    {
        // телепорт назад или кик
        transform.position = lastPos;
    }
    lastPos = transform.position;
    lastCheck = Time.time;
}
```

#### Вариант B. Server authority

Сервер двигает объект, клиент только шлёт ввод.

```csharp
public class ServerMove : NetworkBehaviour
{
    [SyncVar] Vector2 input;

    [Command]
    void CmdInput(Vector2 value) => input = value;

    void Update()
    {
        if (!isServer) return;
        Vector3 move = new Vector3(input.x, 0, input.y) * speed * Time.deltaTime;
        transform.position += move;
    }
}
```

Плюсы: сервер — правда, читы сложнее.
Минусы: задержка ввода, «резиновость» без предсказания. Для персонажа на `CharacterController` требует аккуратности.

#### Вариант C. Prediction (для Rigidbody)

Mirror 96 содержит `PredictedRigidbody` и `LagCompensator`:

- `PredictedRigidbody` — клиент предсказывает физику, сервер присылает коррекции.
- `LagCompensator` — «откат» мира на сервере для честных попаданий.

```csharp
// На префабе: Rigidbody + NetworkIdentity + NetworkTransform + PredictedRigidbody
// В Inspector: mode = Smooth, positionCorrectionThreshold = 0.1
```

Ограничение: `PredictedRigidbody` рассчитан на `Rigidbody`, а не на `CharacterController`. Для персонажа с `CharacterController` в Mirror 96 нет «из коробки» полноценного предсказания — обычно берут client authority + серверную валидацию.

### 9.3 CharacterController: типичные грабли

- `CharacterController.Move` должен вызываться **только у владельца** (вариант A) или **только на сервере** (вариант B). Если и там, и там — движение удваивается и дёргается.
- `NetworkTransform` тоже пишет в `transform`. Не добавляйте второй источник позиции: либо `CharacterController` + `NetworkTransform`, либо свой SyncVar/ручной RPC, но не оба сразу.
- `Physics.SyncTransforms()` помогает при телепорте.
- Не включайте `Rigidbody.isKinematic = false` на объекте с `CharacterController` — конфликт.

### 9.4 Телепорт и респавн

```csharp
using Mirror;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    public NetworkTransformReliable networkTransform;

    [Server]
    public void Respawn(Vector3 position)
    {
        // Двигает на сервере и рассылает клиентам без интерполяции
        networkTransform.ServerTeleport(position, transform.rotation);
    }
}
```

В Mirror 96 у `NetworkTransformBase` есть готовые методы, которые сбрасывают буферы интерполяции и предотвращают «рывок» через полкарты:

- `CmdTeleport(Vector3)` / `CmdTeleport(Vector3, Quaternion)` — клиент с authority просит сервер телепортировать;
- `RpcTeleport(Vector3)` / `RpcTeleport(Vector3, Quaternion)` — сервер рассылает клиентам;
- `ServerTeleport(Vector3, Quaternion)` — сервер двигает объект и рассылает всем.

Вызывайте их вместо простого `transform.position = ...` на объекте с `NetworkTransform`.

---

## 10. Анимация

### 10.1 NetworkAnimator

Компонент синхронизирует **параметры** Animator (float/int/bool) и состояние слоёв. Триггеры автоматически **не** синхронизируются — для них есть `SetTrigger`.

```csharp
using Mirror;
using UnityEngine;

public class PlayerAnimationNet : NetworkBehaviour
{
    public NetworkAnimator networkAnimator;

    // clientAuthority = false: триггер вызывается на сервере
    [Command]
    void CmdAttack()
    {
        networkAnimator.SetTrigger("Attack");
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetMouseButtonDown(0))
            CmdAttack();
    }
}
```

Если `NetworkAnimator.clientAuthority = true`, Command не нужен: владелец дергает триггер локально, и он уходит другим клиентам.

```csharp
void Update()
{
    if (!isOwned) return;
    if (Input.GetMouseButtonDown(0))
        networkAnimator.SetTrigger("Attack");
}
```

### 10.2 Authority

- `clientAuthority = true` — анимации игрока идут от владельца. Подходит для player prefab.
- `clientAuthority = false` — анимации идут с сервера. Подходит для NPC, дверей, объектов мира.
- В текущем player prefab стоит `clientAuthority = false`. Поэтому локальный `AnimatorChanger.DoAttack()` не увиден другими. Либо включите `clientAuthority`, либо вызывайте `networkAnimator.SetTrigger` на сервере (например, в Command).

### 10.3 Синхронизация скорости

`NetworkAnimator` синхронизирует `animator.speed`. Если вы меняете скорость локально, она разойдётся. Меняйте `animator.speed` на сервере — компонент разошлёт значение клиентам.

### 10.4 Альтернатива: свой SyncVar

Для сложных Animator-графов (IK, слои, blend trees) иногда проще синхронизировать 2–3 float/int через `SyncVar` и вызывать `animator.SetFloat` в hook. Это дешевле, чем `NetworkAnimator`, но требует ручной работы.

---

## 11. Рецепты механик

### 11.1 Здоровье и HP-слайдер

В проекте уже есть `HealthSystem` с `[SyncVar] float _SyncHealth` и hook `SyncHealth`. Проблема: слайдер в prefab может быть не назначен, а `healthText` может быть `null`.

#### Вариант A. Всё в `HealthSystem` (минимум изменений)

Требования:

1. В player prefab у `HealthSystem` назначить `hpSlider` (Slider из HUD) и `healthText`.
2. У `Slider` выставить `Max Value = 100` (или `maxHealth`).
3. В `Update` проверять `null`:

```csharp
void Update()
{
    if (!isOwned) return;

    if (healthText != null)
        healthText.text = Mathf.CeilToInt(Health).ToString();

    if (hpSlider != null)
        hpSlider.value = Health;
}
```

4. Hook `SyncHealth` обновляет `Health` у клиентов. В host-режиме hook тоже срабатывает (host — клиент), на выделенном сервере UI нет. `Update` дополнительно обновляет HUD у владельца.

Плюс: мало кода. Минус: UI-логика в игровом компоненте.

#### Вариант B. Отдельный HUD-компонент (рекомендуется)

```csharp
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHud : NetworkBehaviour
{
    public Slider hpSlider;
    public TMP_Text hpText;

    HealthSystem health;

    public override void OnStartLocalPlayer()
    {
        health = GetComponent<HealthSystem>();
        if (hpSlider != null) hpSlider.maxValue = 100f;
        Refresh();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (health == null) return;
        Refresh();
    }

    void Refresh()
    {
        if (hpSlider != null) hpSlider.value = health.Health;
        if (hpText != null) hpText.text = Mathf.CeilToInt(health.Health).ToString();
    }
}
```

Плюсы: UI отдельно от логики; легко добавить ману, стамину, баффы. Минус: один компонент на игрока.

#### Вариант C. События (самый чистый)

`HealthSystem` добавляет:

```csharp
public event System.Action<float, float> OnHealthChanged; // current, max

void SyncHealth(float oldValue, float newValue)
{
    Health = newValue;
    OnHealthChanged?.Invoke(Health, maxHealth);
}

[Server]
public void ChangeHealthValue(float newValue)
{
    _SyncHealth = Mathf.Clamp(newValue, 0, maxHealth);
    OnHealthChanged?.Invoke(_SyncHealth, maxHealth); // для host/сервера
}
```

HUD подписывается в `OnStartLocalPlayer`. Это лучший вариант, если UI сложный.

> **Почему UI может не обновиться:** hook не вызывается на server-only (без клиента), а также если слайдер не назначен или HUD не подписан. В host-режиме hook срабатывает, потому что host — тоже клиент. Если сомневаетесь — обновляйте UI ещё и в мутаторе.

### 11.2 Атака и урон

Текущая схема: `AttackManager` (local player) → `CombatBase.RequestAttack()` → `[Command] CmdAttack()` → сервер делает `OverlapSphere`/`Raycast` → `IDamageable.DealDamage`.

Что добавить для надёжности:

```csharp
[Command]
void CmdAttack(Vector3 aimPoint)
{
    if (!IsAlive()) return;
    if (Time.time < lastAttackTime + currentAttack.cooldown) return;

    // Проверка дистанции до цели
    if (Vector3.Distance(transform.position, aimPoint) > currentAttack.range + 1f) return;

    lastAttackTime = Time.time;
    DoMeleeAttack();
}
```

Рекомендации:

1. **Кулдаун — на сервере.** Клиентский кулдаун только для UI.
2. **Не доверяйте `aimPoint`.** Проверяйте, что точка в пределах дальности, и делайте raycast от серверной позиции игрока.
3. **`hitLayers`** — LayerMask. Не бейте по слою игрока, который атакует.
4. **`QueryTriggerInteraction.Ignore`**, чтобы триггеры не получали урон случайно.
5. **Один источник HP** — `HealthSystem`. `Damageable` (Invector) — отдельный компонент, не смешивайте.
6. **Смерть** — на сервере, затем `ClientRpc` на анимацию/UI, затем респавн или `NetworkServer.Destroy`.

Пример серверной атаки с проверкой линии видимости:

```csharp
[Server]
void DoMeleeAttack()
{
    Vector3 origin = attackOrigin.position + attackOrigin.forward * currentAttack.range * 0.5f;
    Collider[] hits = Physics.OverlapSphere(
        origin, currentAttack.radius, currentAttack.hitLayers, QueryTriggerInteraction.Ignore);

    foreach (Collider hit in hits)
    {
        if (hit.transform.root == transform.root) continue;

        if (!Physics.Linecast(attackOrigin.position, hit.transform.position,
                out RaycastHit block, currentAttack.hitLayers))
        {
            IDamageable dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive())
                dmg.DealDamage(currentAttack.damage, gameObject);
        }
    }
}
```

### 11.3 Подбор, выброс, инвентарь

Это самая большая переделка в проекте. Текущая проблема: `Inventory`, `TakingObject`, `DropObject` — обычные `MonoBehaviour`, без `isOwned`, а предметы — не сетевые объекты. В мультиплеере каждый клиент «подбирает» предмет локально, у остальных он остаётся.

#### Целевая архитектура

```
ItemDefinition (ScriptableObject)
    id, name, icon, prefab, maxStack, type

ItemStack (struct)
    itemId, count

PlayerInventory : NetworkBehaviour
    SyncList<ItemStack> slots

WorldItem : NetworkBehaviour
    itemId, count
    [Server] TryPickup(PlayerInventory)
```

#### `ItemDefinition`

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    public int id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public GameObject worldPrefab;   // префаб с NetworkIdentity + WorldItem
    public int maxStack = 1;
    public ItemType type;
}

public enum ItemType { Resource, Weapon, Consumable, Quest }
```

`ItemDatabase` — обычный `MonoBehaviour` или `ScriptableObject` со списком `ItemDefinition`. На клиенте и сервере он должен быть одинаковым (лежит в Resources или назначен в сцене).

#### `WorldItem`

```csharp
using Mirror;
using UnityEngine;

public class WorldItem : NetworkBehaviour
{
    public int itemId;
    public int count = 1;

    [Server]
    public bool ServerPickup(PlayerInventory inventory)
    {
        if (!inventory.TryAddItem(itemId, count))
            return false;

        NetworkServer.Destroy(gameObject);
        return true;
    }
}
```

#### `PlayerInventory`

```csharp
using Mirror;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public const int MaxSlots = 8;
    public ItemDatabase database;

    public readonly SyncList<ItemStack> slots = new SyncList<ItemStack>();

    public override void OnStartServer()
    {
        for (int i = 0; i < MaxSlots; i++)
            slots.Add(default);
    }

    [Server]
    public bool TryAddItem(int itemId, int count)
    {
        ItemDefinition def = database.Get(itemId);
        if (def == null || count <= 0) return false;

        // 1. Докладываем в существующие стаки
        for (int i = 0; i < slots.Count; i++)
        {
            ItemStack s = slots[i];
            if (s.itemId != itemId || s.count >= def.maxStack) continue;

            int space = def.maxStack - s.count;
            int add = Mathf.Min(space, count);
            s.count += add;
            slots[i] = s;
            count -= add;
            if (count == 0) return true;
        }

        // 2. Ищем пустые слоты
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].count != 0) continue;

            int add = Mathf.Min(def.maxStack, count);
            slots[i] = new ItemStack { itemId = itemId, count = add };
            count -= add;
            if (count == 0) return true;
        }

        return false; // не поместилось
    }

    [Server]
    public bool TryRemoveItem(int itemId, int count)
    {
        // сначала проверяем, что хватает
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].itemId == itemId) total += slots[i].count;

        if (total < count) return false;

        for (int i = 0; i < slots.Count && count > 0; i++)
        {
            ItemStack s = slots[i];
            if (s.itemId != itemId) continue;

            int remove = Mathf.Min(s.count, count);
            s.count -= remove;
            count -= remove;
            slots[i] = s.count == 0 ? default : s;
        }

        return true;
    }
}
```

#### Подбор: Command на игроке

```csharp
public class PlayerPickup : NetworkBehaviour
{
    public PlayerInventory inventory;
    public float pickupRange = 3f;

    [Command]
    public void CmdPickup(uint worldItemNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(worldItemNetId, out NetworkIdentity id))
            return;

        WorldItem item = id.GetComponent<WorldItem>();
        if (item == null) return;

        if (Vector3.Distance(transform.position, item.transform.position) > pickupRange + 1f)
            return;

        item.ServerPickup(inventory);
    }
}
```

Клиент:

```csharp
void Update()
{
    if (!isLocalPlayer) return;
    if (!Input.GetKeyDown(KeyCode.E)) return;

    if (Physics.Raycast(cameraTransform.position, cameraTransform.forward,
            out RaycastHit hit, pickupRange, pickupMask))
    {
        NetworkIdentity id = hit.collider.GetComponentInParent<NetworkIdentity>();
        if (id != null)
            CmdPickup(id.netId);
    }
}
```

#### Выброс

```csharp
[Command]
public void CmdDrop(int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

    ItemStack stack = inventory.slots[slotIndex];
    if (stack.count <= 0) return;

    ItemDefinition def = inventory.database.Get(stack.itemId);
    if (def == null || def.worldPrefab == null) return;

    Vector3 pos = transform.position + transform.forward * 1.2f;
    GameObject go = Instantiate(def.worldPrefab, pos, Quaternion.identity);

    WorldItem world = go.GetComponent<WorldItem>();
    world.itemId = stack.itemId;
    world.count = stack.count;

    NetworkServer.Spawn(go);
    inventory.slots[slotIndex] = default;
}
```

> `def.worldPrefab` должен быть в `spawnPrefabs` (или зарегистрирован через `RegisterPrefab`).

#### Что делать со старым кодом

1. `Inventory` → заменить на `PlayerInventory` (`NetworkBehaviour` + `SyncList`).
2. `TakingObject` → оставить только raycast и ввод, вызывать `CmdPickup`.
3. `DropObject` → заменить на `CmdDrop`.
4. `ResourcesObject`, `Weapon`, `SecondWeapon` (MonoBehaviour-данные) → заменить на `ItemDefinition` + `ItemStack`.
5. `InventorySlot` (UI) → привязать к `SyncList`, обновлять по `OnSet`/`OnChange`.
6. Предметы на земле → `WorldItem` с `NetworkIdentity`, либо scene-объекты с `sceneId`.

Это можно делать поэтапно: сначала ресурсы (проще всего), потом оружие, потом экипировка.

### 11.4 NPC и AI

Правило: **AI считается только на сервере**. Клиенты видят результат.

```csharp
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class NpcAI : NetworkBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public float detectRange = 10f;

    void Update()
    {
        if (!isServer) return; // критично

        // найти ближайшего игрока
        GameObject target = FindNearestPlayer();
        if (target == null) return;

        agent.SetDestination(target.transform.position);
        // NetworkAnimator на этом же объекте разошлёт параметр клиентам
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
}
```

Настройки:

- `NetworkTransform`: `syncDirection = ServerToClient`, `onlySyncOnChange = true`.
- `NetworkAnimator.clientAuthority = false` — анимации с сервера.
- `NavMeshAgent` можно оставить включённым на клиентах, но тогда он будет «сражаться» с `NetworkTransform`. Лучше выключать на клиентах:

```csharp
public override void OnStartClient()
{
    if (!isServer) agent.enabled = false;
}
```

- Для NPC с `NetworkIdentity` в сцене нужен `sceneId`.
- Для спавна NPC в рантайме — префаб в `spawnPrefabs` + `NetworkServer.Spawn`.

### 11.5 Чат

Два подхода.

#### A. Через `NetworkMessage` (до спавна игрока тоже работает)

См. [7.4](#74-networkmessage-сообщения-вне-объектов).

#### B. Через `Command` на игроке

```csharp
public class PlayerChat : NetworkBehaviour
{
    [Command]
    public void CmdSend(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 200) return;
        RpcReceive($"[{connectionToClient.connectionId}] {text}");
    }

    [ClientRpc]
    void RpcReceive(string line)
    {
        ChatUI.Instance?.AddLine(line);
    }
}
```

Плюсы B: не нужен отдельный `NetworkManager`, работает после спавна. Минусы: нельзя писать до появления игрока.

Анти-спам:

```csharp
float nextMessageTime;

[Command]
public void CmdSend(string text)
{
    if (Time.time < nextMessageTime) return;
    nextMessageTime = Time.time + 1f; // не чаще 1 сообщения в секунду
    // ...
}
```

### 11.6 Лобби и комнаты

#### A. Простое лобби на `NetworkRoomManager`

В Mirror есть готовый `NetworkRoomManager` (`Assets/Mirror/Components/NetworkRoomManager.cs`).

1. Создать `NetworkManager` → заменить на `NetworkRoomManager` (или наследника).
2. Настроить:
   - `Room Scene` — сцена лобби;
   - `Gameplay Scene` — игровая сцена;
   - `Room Player Prefab` — префаб с `NetworkRoomPlayer`;
   - `Min Players` — минимум для старта.
3. `NetworkRoomPlayer` имеет `CmdChangeReadyState(bool)` и `readyToBegin`.
4. Готовый пример: `Assets/Mirror/Examples/Room/`.

```csharp
using Mirror;
using UnityEngine;

public class RoomUi : MonoBehaviour
{
    public void OnReadyClicked(bool ready)
    {
        NetworkClient.localPlayer.GetComponent<NetworkRoomPlayer>().CmdChangeReadyState(ready);
    }
}
```

#### B. Своё лобби (гибче)

- `SyncList<LobbyPlayer>` в отдельном `NetworkBehaviour` (например, на объекте `LobbyManager` в сцене).
- `LobbyPlayer` — struct: `connectionId`, `name`, `ready`, `characterId`.
- Команды: `CmdSetReady`, `CmdSetCharacter`, `CmdStartGame`.
- Сервер проверяет, что все готовы, и делает `ServerChangeScene("Game")`.

Плюсы: полный контроль. Минусы: больше кода.

#### C. Выбор персонажа

Пример: `Assets/Mirror/Examples/CharacterSelection/`.

Ключевая идея: клиент отправляет `NetworkMessage` с выбранным персонажем, сервер создаёт нужный prefab и вызывает `NetworkServer.AddPlayerForConnection`.

```csharp
public struct CreateCharacterMessage : NetworkMessage
{
    public int characterId;
}

public class CharSelectManager : NetworkManager
{
    public GameObject[] characterPrefabs;
    public int SelectedCharacterId; // выставляется в UI выбора

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<CreateCharacterMessage>(OnCreateCharacter);
    }

    // Клиент отправляет выбор сразу после подключения.
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        NetworkClient.Send(new CreateCharacterMessage { characterId = SelectedCharacterId });
    }

    void OnCreateCharacter(NetworkConnectionToClient conn, CreateCharacterMessage msg)
    {
        if (msg.characterId < 0 || msg.characterId >= characterPrefabs.Length) return;

        Transform start = GetStartPosition();
        GameObject go = start != null
            ? Instantiate(characterPrefabs[msg.characterId], start.position, start.rotation)
            : Instantiate(characterPrefabs[msg.characterId]);

        NetworkServer.AddPlayerForConnection(conn, go);
    }
}
```

> В инспекторе `NetworkManager` выключите **`Auto Create Player`**, иначе Mirror создаст дефолтного игрока из `playerPrefab` раньше, чем придёт сообщение. В примере `CharacterSelection` сделано именно так: `autoCreatePlayer = false`, отправка сообщения в `OnClientConnect()` после `base.OnClientConnect()`. Смотрите `Assets/Mirror/Examples/CharacterSelection/Scripts/NetworkManagerCharacterSelection.cs`.

### 11.7 Выбор персонажа и точки спавна

- `NetworkStartPosition` — компонент-маркер. Поставьте несколько объектов в сцене.
- `NetworkManager.startPositions` — заполняется автоматически.
- `playerSpawnMethod`: `Random` или `RoundRobin`.
- Своя логика:

```csharp
public override Transform GetStartPosition()
{
    // например, по команде игрока
    return teamSpawnPoints[teamId];
}
```

Для выбора точки на клиенте — `Command` с индексом, сервер проверяет, что индекс валиден.

### 11.8 Взаимодействие и подсказки UI

Подсказка «Нажмите E» должна быть **только у локального игрока** и **только для ближайшего объекта**.

```csharp
public class InteractionHint : MonoBehaviour
{
    public GameObject hintRoot;
    public float range = 3f;
    public LayerMask mask;

    Transform cam;

    void Start() => cam = Camera.main != null ? Camera.main.transform : null;

    void Update()
    {
        if (cam == null) return;

        bool found = Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask)
                     && hit.collider.GetComponentInParent<WorldItem>() != null;

        hintRoot.SetActive(found);
    }
}
```

Важно: этот компонент должен быть выключен на чужих player prefab. Проще всего повесить его на HUD локального игрока и включать в `OnStartLocalPlayer`.

### 11.9 Смерть и респавн

```csharp
public class PlayerDeath : NetworkBehaviour
{
    public HealthSystem health;
    public GameObject deathEffect;

    [Server]
    public void ServerDie()
    {
        RpcOnDeath();
        // вариант 1: респавн через N секунд
        Invoke(nameof(ServerRespawn), 3f);
        // вариант 2: NetworkServer.Destroy(gameObject) и создать нового игрока
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        // анимация, звук, UI
    }

    [Server]
    void ServerRespawn()
    {
        health.ChangeHealthValue(100f);
        RpcOnRespawn();
    }

    [ClientRpc]
    void RpcOnRespawn()
    {
        // вернуть управление, камеру
    }
}
```

Не уничтожайте player object на клиенте вручную. Если игрок должен «исчезнуть» — `NetworkServer.Destroy`, если «лежать» — выключите управление и коллайдеры на сервере.

---

## 12. UI в мультиплеере

### 12.1 Главное правило

**Каждый клиент видит свой UI, а не UI всех игроков.**

- HUD (HP, инвентарь, чат) — только `isLocalPlayer`.
- World-space health bar над чужим игроком — можно, но он должен читать `SyncVar` этого игрока и обновляться у всех.
- Не используйте `FindObjectOfType<Player>()` — найдёте чужого. Используйте `NetworkClient.localPlayer`.

### 12.2 Поиск локального игрока

```csharp
GameObject local = NetworkClient.localPlayer?.gameObject;
```

Или в `NetworkBehaviour`:

```csharp
if (!isLocalPlayer) return;
```

### 12.3 World-space UI над игроком

```csharp
public class NameTag : NetworkBehaviour
{
    public TMP_Text label;
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName;

    void OnNameChanged(string oldValue, string newValue) => label.text = newValue;

    void LateUpdate()
    {
        // поворот к камере
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }
}
```

`Camera.main` ищет по тегу `MainCamera`. В этом проекте камера `Untagged` — либо поставьте тег, либо передайте ссылку.

### 12.4 Слайдеры и события

`Slider.onValueChanged` — локальное событие. Не используйте его для отправки данных на сервер без проверки: игрок может двигать слайдер. Для HP слайдер должен быть **только для чтения** (interactable = false).

---

## 13. Безопасность

Мир Mirror не защищает от читов автоматически. Минимальный чек-лист:

- [ ] Все `[Command]` проверяют: жив ли игрок, есть ли предмет, кулдаун, дистанция, ownership.
- [ ] Никаких `[Command(requiresAuthority = false)]` без причины.
- [ ] Сервер не доверяет позиции/урону/цене из клиента.
- [ ] Кулдауны хранятся на сервере (`lastAttackTime`), а не только в UI.
- [ ] `NetworkServer.spawned` используется для поиска цели, а не `GameObject.Find`.
- [ ] Проверка `Vector3.Distance` для подбора/удара.
- [ ] Лимит длины строк (чат, имена) — иначе спам/переполнение.
- [ ] Не отправляйте в `SyncVar` пароли, токены, скрытые характеристики.
- [ ] Используйте `NetworkAuthenticator` для логина (пример: `Assets/Mirror/Examples/Chat/Scripts/ChatAuthenticator.cs`).
- [ ] `exceptionsDisconnect = true` (по умолчанию) — не отключайте без причины.
- [ ] Для соревновательных игр — серверная валидация движения и `LagCompensator`.
- [ ] Не логируйте в релизе чувствительные данные.

### 13.1 Аутентификатор

```csharp
using Mirror;
using UnityEngine;

public struct AuthMessage : NetworkMessage
{
    public string password;
}

public class SimpleAuthenticator : NetworkAuthenticator
{
    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthMessage>(OnAuth);
    }

    void OnAuth(NetworkConnectionToClient conn, AuthMessage msg)
    {
        if (msg.password != "secret") // так делать нельзя, это пример
        {
            conn.Disconnect();
            return;
        }
        ServerAccept(conn);
    }

    public override void OnClientAuthenticate()
    {
        NetworkClient.Send(new AuthMessage { password = "secret" });
    }
}
```

> В реальном проекте пароль проверяется на сервере по хешу, а не строкой в коде.

---

## 14. Производительность

| Что | Рекомендация |
|---|---|
| `sendRate` | 30–60. Для медленных игр 20. |
| `NetworkTransform.syncInterval` | 0.05–0.1 для игроков, 0.2+ для NPC. |
| `onlySyncOnChange` | Включить для статичных объектов. |
| `syncPosition/Rotation/Scale` | Отключить лишнее (scale почти всегда не нужен). |
| Interest Management | Для больших миров: `DistanceInterestManagement` или `SpatialHashing3DInterestManagement`. |
| `SyncList` | Не менять сотни элементов за кадр. |
| RPC | Не слать каждый кадр; для эффектов — батчинг. |
| `Camera.main` | Кэшировать. |
| `GetComponent` | Кэшировать в `Awake`. |
| `Physics.OverlapSphere` | `NonAlloc` версия + LayerMask. |
| Строки | Не конкатенировать в `Update`; для UI использовать кэш. |
| Instantiate/Destroy | Пул объектов. |
| `Debug.Log` | Убрать из горячих путей. |

### 14.1 Interest Management

Компонент на `NetworkManager` (или отдельном объекте):

- `DistanceInterestManagement` — клиент видит объекты в радиусе.
- `SceneInterestManagement` — только объекты своей сцены.
- `SpatialHashing3DInterestManagement` — для больших миров.
- `TeamInterestManagement` — свои/чужие.

Важно: interest management влияет на то, какие объекты **спавнятся** у клиента. Ссылки на «невидимые» объекты могут быть `null`.

### 14.2 Профилирование

- `NetworkStatistics` — пакеты/байты в секунду.
- `NetworkManagerHUD` — базовый статус.
- Unity Profiler → Network → Mirror.
- `NetworkTransform` overlay (`showOverlay`) — для отладки интерполяции.

---

## 15. Отладка и типовые ошибки

| Симптом | Причина | Решение |
|---|---|---|
| `Transport not assigned` / Host не стартует | у `NetworkManager` пустое поле `transport` | добавить `KcpTransport` и назначить |
| `Prefab not registered` / клиент не видит объект | префаб не в `spawnPrefabs` | добавить префаб или `NetworkClient.RegisterPrefab` |
| `Command called on unspawned object` | Command вызван до спавна / после Destroy | вызывать после `OnStartClient` |
| `Command requires authority` | у клиента нет authority на объекте | Command на player + `netId`, либо `AssignClientAuthority` |
| `RPC called on unspawned object` | RPC до спавна | вызывать после спавна; проверить `netId != 0` |
| Hook не сработал | на server-only hook не вызывается; в host — вызывается | не полагаться на hook в серверной логике; UI обновлять и в мутаторе |
| Клиент не видит чужого игрока | player prefab без `NetworkIdentity` / не заспавнен | проверить prefab и `AddPlayerForConnection` |
| Все клиенты «подбирают» один предмет | логика в `MonoBehaviour` без `isOwned` | перенести в `Command` на сервере |
| Предмет исчез у одного, остался у других | предмет не сетевой | `WorldItem : NetworkBehaviour` + `NetworkServer.Destroy` |
| Анимация не видна другим | `NetworkAnimator.clientAuthority = false`, триггер локальный | включить `clientAuthority` или Command→Rpc |
| Движение дёргается | двигают и клиент, и сервер | оставить один источник |
| Движение «плывёт» | интерполяция + низкий `sendRate` | поднять `sendRate`, проверить `syncInterval` |
| NPC стоит у клиента | `NavMeshAgent` включён на клиенте | выключать на клиентах, двигать на сервере |
| `NullReferenceException` в hook | UI ещё не создан | проверять `null`, подписываться в `OnStartClient` |
| `SyncVar` не обновляется | меняют на клиенте | менять только на сервере |
| `SyncList` не обновляется | пересоздали ссылку / меняют на клиенте | `readonly`, менять на сервере |
| Ошибка Weaver | неподдерживаемый тип в SyncVar/RPC | использовать struct/примитивы, `SyncList` вместо `List` |
| Host работает, клиент — нет | серверный код не защищён `isServer`, клиентский — `isLocalPlayer` | проверить guard-ы |
| `Camera.main` = null | камера без тега `MainCamera` | поставить тег или прямую ссылку |
| UI показывает чужое HP | нет `isLocalPlayer` | добавить проверку |
| Сцена не грузится у клиента | `offlineScene`/`onlineScene` не настроены | заполнить в `NetworkManager` |
| Объект есть у сервера, нет у клиента | `sceneId == 0` у scene-объекта | сохранить сцену / пересоздать `NetworkIdentity` |

### 15.1 Куда смотреть в исходниках

- `Assets/Mirror/Core/NetworkBehaviour.cs` — `isServer`, `isClient`, `isOwned`, `authority`, `syncDirection`, `syncMode`, `syncInterval`.
- `Assets/Mirror/Core/NetworkServer.cs` — `Spawn`, `Destroy`, `UnSpawn`, `spawned`.
- `Assets/Mirror/Core/NetworkClient.cs` — `Connect`, `RegisterPrefab`, `spawned`.
- `Assets/Mirror/Core/NetworkManager.cs` — `OnServerAddPlayer`, `startPositions`, `spawnPrefabs`.
- `Assets/Mirror/Core/SyncList.cs` — события `OnAdd`, `OnSet`, `OnRemove`, `OnClear`, `Callback`.
- `Assets/Mirror/Components/NetworkTransform/NetworkTransformBase.cs` — `syncDirection`, `interpolate`, `Teleport`.
- `Assets/Mirror/Components/NetworkAnimator.cs` — `clientAuthority`, `SetTrigger`.

### 15.2 Полезные приёмы

```csharp
// Кто я?
Debug.Log($"{name} server={isServer} client={isClient} owned={isOwned} local={isLocalPlayer} netId={netId}");

// Что заспавнено?
foreach (var kv in NetworkServer.spawned)
    Debug.Log($"{kv.Key}: {kv.Value.name}");

// Есть ли объект у клиента?
if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity id)) { ... }
```

---

## 16. Карта проекта и план миграции

### 16.1 Текущее состояние

| Файл | Тип | Проблема | Что делать |
|---|---|---|---|
| `Assets/Scripts/Global/NetMan.cs` | `NetworkManager` | нет транспорта; `OnCreateCharacter` не используется | добавить `KcpTransport`; решить, нужен ли кастомный спавн |
| `Assets/Scripts/Player/NetMovement.cs` | `NetworkBehaviour` | движение только у `isOwned`; нет серверной валидации | оставить client authority + добавить проверку скорости |
| `Assets/Scripts/Player/NetPlayerInformation.cs` | `NetworkBehaviour` | `Awake` без null-проверок | добавить guard-ы |
| `Assets/Scripts/Player/Animation/AnimatorChanger.cs` | `NetworkBehaviour` | `[Client]` + локальные триггеры, `NetworkAnimator.clientAuthority = false` | включить `clientAuthority` или Command→Rpc |
| `Assets/Scripts/Combat/AttackManager.cs` | `NetworkBehaviour` | нет проверки UI/курсора; атака по ЛКМ конфликтует с подбором | добавить `EventSystem.current.IsPointerOverGameObject()` |
| `Assets/Scripts/Combat/CombatBase.cs` | `NetworkBehaviour` | кулдаун на сервере есть, но нет проверки дистанции/ЛОС | добавить валидацию |
| `Assets/Scripts/Combat/HealthSystem.cs` | `NetworkBehaviour` | `healthText.text` без null; UI в `Update` | вынести HUD или добавить null-guard |
| `Assets/Scripts/Combat/Damageable.cs` | `MonoBehaviour` | второй источник HP, не сетевой | не использовать для игроков; либо объединить с `HealthSystem` |
| `Assets/Scripts/Player/Inventory.cs` | `MonoBehaviour` | `List<ResourcesObject>`, не сетевой | заменить на `PlayerInventory : NetworkBehaviour` + `SyncList` |
| `Assets/Scripts/Player/TakingObject.cs` | `MonoBehaviour` | нет `isOwned`; `Camera` null; raycast каждый кадр | оставить raycast, вызывать `CmdPickup` |
| `Assets/Scripts/Player/DropObject.cs` | `MonoBehaviour` | `new Weapon()` (MonoBehaviour!), нет сети | заменить на `CmdDrop` |
| `Assets/VillagerOLD/Scripts/Weapon.cs` | `MonoBehaviour` | данные предмета как компонент | заменить на `ItemDefinition` |
| `Assets/VillagerOLD/Scripts/ResourcesObject.cs` | `MonoBehaviour` | `IsFull`/`AddCount` с багами | заменить на `ItemStack` |
| `Assets/Prefabs/Weapons/GroundedObject.prefab` | prefab | нет `NetworkIdentity` | добавить `NetworkIdentity` + `WorldItem` |
| `Assets/Prefabs/PlayerObjects.prefab` | prefab | `hpSlider`/`healthText` могут быть не назначены; камера Untagged | назначить ссылки, поставить тег |
| `Assets/Scenes/TestScene.unity` | scene | `spawnPrefabs` пуст; предметы не сетевые | заполнить `spawnPrefabs`, добавить `NetworkIdentity` |

### 16.2 Поэтапный план

**Этап 1. База (1–2 часа)**

1. `KcpTransport` на `Netman`.
2. Проверить host + 2 клиента.
3. Назначить `hpSlider` и `healthText` в player prefab.
4. Исправить `Camera` tag / ссылку.

**Этап 2. Игрок (полдня)**

1. `PlayerHud` для HP (или допилить `HealthSystem`).
2. `NetworkAnimator.clientAuthority = true` для player prefab.
3. Проверить, что атака и анимация видны другим.
4. Добавить `EventSystem` проверку в `AttackManager`.

**Этап 3. Предметы (1–2 дня)**

1. `ItemDefinition` + `ItemDatabase`.
2. `ItemStack` + `PlayerInventory : NetworkBehaviour` с `SyncList`.
3. `WorldItem : NetworkBehaviour`.
4. `CmdPickup` / `CmdDrop`.
5. Переделать UI инвентаря на `SyncList`-события.
6. Удалить/отключить старые `Inventory`, `TakingObject`, `DropObject`, `ResourcesObject`.

**Этап 4. NPC и мир (1 день)**

1. NPC: `isServer` guard, `NavMeshAgent` только на сервере.
2. Scene-объекты: `NetworkIdentity` + `sceneId`.
3. `spawnPrefabs` для runtime-объектов.

**Этап 5. Лобби и полировка (по желанию)**

1. `NetworkRoomManager` или своё лобби.
2. Синхронизация имён/скинов.
3. Interest management, если мир большой.

### 16.3 Что можно не переделывать

- `NetMovement` — уже `NetworkBehaviour` и работает через `isOwned`. Достаточно добавить валидацию.
- `CombatBase` / `AttackData` — архитектура нормальная. Нужна валидация и связь с экипировкой.
- `HealthSystem` — почти готов. Нужен UI и null-guard.
- `NetPlayerInformation` — удобный агрегатор ссылок. Добавить guard-ы.

---

## 17. Шпаргалка

### 17.1 Атрибуты

| Атрибут | Где | Направление | Замечание |
|---|---|---|---|
| `[SyncVar]` | поле | server → clients | hook `(old,new)` у клиентов и host, не на server-only |
| `[SyncVar(hook = nameof(F))]` | поле | server → clients | `F(T old, T new)` |
| `[Command]` | метод | client → server | требует authority |
| `[Command(requiresAuthority = false)]` | метод | client → server | осторожно |
| `[ClientRpc]` | метод | server → all | `includeOwner` |
| `[TargetRpc]` | метод | server → one | первый параметр `NetworkConnectionToClient` |
| `[Server]` | метод | только сервер | warning при вызове не на сервере |
| `[ServerCallback]` | метод | только сервер | без warning |
| `[Client]` | метод | только клиент | warning |
| `[ClientCallback]` | метод | только клиент | без warning |
| `[SyncVar]` + `syncDirection` | компонент | client → server | `SyncDirection.ClientToServer` |

### 17.2 Свойства

| Свойство | Значение |
|---|---|
| `isServer` | сервер/host |
| `isClient` | клиент/host |
| `isOwned` | у владельца |
| `isLocalPlayer` | локальный player prefab |
| `authority` | право менять sync-состояние (зависит от `syncDirection`) |
| `netId` | сетевой ID |
| `netIdentity` | `NetworkIdentity` |
| `connectionToClient` | соединение владельца (на сервере) |
| `connectionToServer` | соединение с сервером (на клиенте) |
| `syncDirection` | `ServerToClient` / `ClientToServer` |
| `syncMode` | `Observers` / `Owner` |
| `syncInterval` | частота синхронизации компонента |

### 17.3 Методы

| Метод | Где |
|---|---|
| `NetworkServer.Spawn(go)` | сервер |
| `NetworkServer.Spawn(go, conn)` | сервер, с authority |
| `NetworkServer.Destroy(go)` | сервер |
| `NetworkServer.UnSpawn(go)` | сервер |
| `NetworkServer.AddPlayerForConnection(conn, go)` | сервер |
| `NetworkServer.ReplacePlayerForConnection(conn, go)` | сервер |
| `NetworkServer.SendToAll(msg)` | сервер |
| `NetworkServer.RegisterHandler<T>(handler)` | сервер |
| `NetworkClient.Send(msg)` | клиент |
| `NetworkClient.RegisterHandler<T>(handler)` | клиент |
| `NetworkClient.RegisterPrefab(prefab)` | клиент |
| `NetworkClient.localPlayer` | клиент |
| `identity.AssignClientAuthority(conn)` | сервер |
| `NetworkManager.StartHost/StartServer/StartClient` | — |
| `NetworkManager.ServerChangeScene(name)` | сервер |

### 17.4 Проверка «я всё сделал правильно»

- [ ] Объект имеет `NetworkIdentity`.
- [ ] Префаб в `spawnPrefabs` (если runtime).
- [ ] Scene-объект имеет `sceneId != 0`.
- [ ] SyncVar меняется только на сервере.
- [ ] Command проверяет authority и входные данные.
- [ ] UI только у `isLocalPlayer`.
- [ ] Анимация и звук идут через RPC или `NetworkAnimator`.
- [ ] Нет `Destroy` сетевых объектов на клиенте.
- [ ] Нет `Camera.main` в `Update`.
- [ ] Проверено на host и на двух клиентах.

---

## 18. Ссылки и что читать дальше

### 18.1 Официальная документация

- Общий вход: <https://mirror-networking.gitbook.io/docs/>
- `NetworkBehaviour`: <https://mirror-networking.gitbook.io/docs/guides/networkbehaviour>
- SyncVar: <https://mirror-networking.gitbook.io/docs/guides/synchronization/syncvars>
- SyncList: <https://mirror-networking.gitbook.io/docs/guides/synchronization/synclists>
- Commands: <https://mirror-networking.gitbook.io/docs/guides/remote-actions/commands>
- ClientRpc: <https://mirror-networking.gitbook.io/docs/guides/remote-actions/client-rpc>
- TargetRpc: <https://mirror-networking.gitbook.io/docs/guides/remote-actions/target-rpc>
- NetworkManager: <https://mirror-networking.gitbook.io/docs/components/network-manager>
- NetworkIdentity: <https://mirror-networking.gitbook.io/docs/components/network-identity>
- NetworkTransform: <https://mirror-networking.gitbook.io/docs/components/network-transform>
- NetworkAnimator: <https://mirror-networking.gitbook.io/docs/components/network-animator>
- NetworkRoomManager: <https://mirror-networking.gitbook.io/docs/components/network-room-manager>
- Interest Management: <https://mirror-networking.gitbook.io/docs/guides/interest-management>
- Authentication: <https://mirror-networking.gitbook.io/docs/components/network-authenticators>
- Prediction: исходники `Assets/Mirror/Core/Prediction/` и `Assets/Mirror/Components/PredictedRigidbody/` (отдельной страницы в документации может не быть)
- Lag Compensation: <https://mirror-networking.gitbook.io/docs/manual/general/lag-compensation>

### 18.2 Исходники и примеры (локально)

Все примеры лежат в `Assets/Mirror/Examples/`. Начинать лучше с них:

| Пример | Чему учит | Путь |
|---|---|---|
| **Basic** | минимальный NetworkManager, движение, UI | `Assets/Mirror/Examples/Basic/` |
| **Tanks** | стрельба, снаряды, серверный урон | `Assets/Mirror/Examples/Tanks/` |
| **Pong** | простой сетевой мяч, счёт | `Assets/Mirror/Examples/Pong/` |
| **Chat** | `NetworkMessage`, аутентификатор, UI | `Assets/Mirror/Examples/Chat/` |
| **Room** | лобби, ready, смена сцены | `Assets/Mirror/Examples/Room/` |
| **PickupsDropsChilds** | подбор/выброс, экипировка, `SyncVar` | `Assets/Mirror/Examples/PickupsDropsChilds/` |
| **SyncDirection** | `ClientToServer` синхронизация | `Assets/Mirror/Examples/SyncDirection/` |
| **TopDownShooter** | стрельба, камера, HUD | `Assets/Mirror/Examples/TopDownShooter/` |
| **CharacterSelection** | выбор персонажа, кастомный спавн | `Assets/Mirror/Examples/CharacterSelection/` |
| **LagCompensation** | откат для попаданий | `Assets/Mirror/Examples/LagCompensation/` |
| **Snapshot Interpolation** | интерполяция | `Assets/Mirror/Examples/Snapshot Interpolation/` |
| **AdditiveScenes** | аддитивные сцены | `Assets/Mirror/Examples/AdditiveScenes/` |
| **MultipleMatches** | несколько матчей на одном сервере | `Assets/Mirror/Examples/MultipleMatches/` |

### 18.3 Что читать в исходниках

1. `Assets/Mirror/Examples/Basic/Scripts/Player.cs` — эталон простого игрока.
2. `Assets/Mirror/Examples/Tanks/Scripts/Tank.cs` — Command + серверная стрельба.
3. `Assets/Mirror/Examples/PickupsDropsChilds/Scripts/PickupsDropsChilds.cs` — `SyncVar` + child objects.
4. `Assets/Mirror/Examples/SyncDirection/Player.cs` — `syncDirection`.
5. `Assets/Mirror/Examples/Chat/Scripts/ChatNetworkManager.cs` — `NetworkMessage`.
6. `Assets/Mirror/Examples/Room/Scripts/NetworkRoomManagerExt.cs` — лобби.
7. `Assets/Mirror/Core/NetworkBehaviour.cs` — база.
8. `Assets/Mirror/Core/NetworkServer.cs` — спавн/уничтожение.

### 18.4 Внешние материалы

- GitHub Mirror: <https://github.com/MirrorNetworking/Mirror>
- Discord Mirror: <https://discord.gg/mirror>
- Форум Unity → Multiplayer: <https://discussions.unity.com/c/multiplayer/>
- Gaffer on Games, snapshot interpolation: <https://gafferongames.com/post/snapshot_interpolation/>
- Valve, lag compensation: <https://developer.valvesoftware.com/wiki/Lag_Compensation>
- Gabriel Gambetta, client-side prediction: <https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html>

---

## Приложение A. Мини-шаблон нового сетевого компонента

```csharp
using Mirror;
using UnityEngine;

public class MyNetworkComponent : NetworkBehaviour
{
    [Header("Sync")]
    [SyncVar(hook = nameof(OnValueChanged))]
    public int value;

    [Header("Refs")]
    public Transform firePoint;

    readonly SyncList<int> _items = new SyncList<int>();

    // ---------- life cycle ----------

    public override void OnStartServer()
    {
        // инициализация серверных данных
        _items.Add(1);
    }

    public override void OnStartClient()
    {
        // подписки UI, кэш
    }

    public override void OnStartLocalPlayer()
    {
        // ввод, камера, HUD
    }

    // ---------- state ----------

    void OnValueChanged(int oldValue, int newValue)
    {
        // обновить UI у клиентов
    }

    [Server]
    public void ServerSetValue(int newValue)
    {
        value = newValue;
        // на server-only hook не вызовется — обновить локально при необходимости
    }

    // ---------- actions ----------

    [Command]
    void CmdDoSomething(int arg)
    {
        if (!isServer) return;           // защита от вызова не на сервере
        if (arg < 0) return;             // валидация

        // серверная логика
        RpcFx();
    }

    [ClientRpc]
    void RpcFx()
    {
        // визуальный эффект у всех
    }

    // ---------- input ----------

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.F))
            CmdDoSomething(1);
    }
}
```

## Приложение B. Порядок действий при «не работает»

1. В консоли есть ошибки? Сначала исправьте их.
2. `NetworkManager` один в сцене? Транспорт назначен?
3. Объект заспавнен? `netId != 0`?
4. Префаб в `spawnPrefabs`?
5. `isServer`/`isClient`/`isOwned`/`isLocalPlayer` — те ли значения?
6. SyncVar меняется на сервере?
7. Command вызывается на объекте с authority?
8. UI обновляется у `isLocalPlayer`?
9. Проверьте host и отдельного клиента.
10. Сравните с ближайшим примером в `Assets/Mirror/Examples/`.

---

*Файл создан как учебный материал. Код в примерах — шаблоны для адаптации, а не готовые файлы проекта. Перед копированием в проект проверяйте совместимость с текущими скриптами и правилами проекта.*
