# Як запустити FarmsteadVR на Meta Quest 3 (з Mac)

Звірено зі сторінками Meta станом на жовтень 2026 (сторінки оновлені 2026-10-19). Пункти, яких на сторінках Meta немає, позначені **[не з документації]**. Їх треба перевірити під час виконання.

## Головне, що треба знати

- Шлях гри на окуляр: **Unity на Mac → файл `.apk` → кабель USB-C → Quest 3**.
- На Mac **не працює Meta Horizon Link** (режим «натиснув Play, а гра йде в окулярі», тільки Windows). Тому кожна перевірка в окулярі означає нову збірку.
- Через це логіку гри перевіряємо на Mac кнопкою Play. В окуляр заходимо раз на кілька кроків.
- Без окуляра можна тестувати в **Meta XR Simulator**. Він працює на Mac з Apple Silicon, у тебе M1 Pro.
- Перший раз піде 2–3 години, здебільшого на завантаження.

---

## Частина 1. Підготовка Mac (один раз, робиш ти)

### 1.1 Додай Android-модуль у Unity (зараз його немає)

На твоєму Mac у Unity 6000.6.10f1 встановлені лише Mac і WebGL. Без Android збирати під Quest неможливо.

1. Відкрий **Unity Hub** → **Installs**.
2. Біля версії `6000.6.10f1` натисни шестерню (або «…») → **Add modules**.
3. Познач **Android Build Support**. Під ним з'являться **OpenJDK** і **Android SDK & NDK Tools**, познач їх теж. Сторінка Meta називає лише Android Build Support, а решту Unity встановлює для нього сам. **[не з документації]**
4. Натисни **Install** і дочекайся завершення.

Android Studio, яка вже стоїть на Mac, для Unity не потрібна, Unity використає власний SDK. Нічого в ній не змінюй.

### 1.2 Акаунт розробника Meta

1. Зайди на <https://developers.meta.com> і увійди тим самим акаунтом Meta, що на твоєму Quest 3.
2. Створи акаунт розробника (організацію), якщо Meta попросить. Без нього режим розробника не вмикається.

### 1.3 Режим розробника на Quest 3

1. На телефоні відкрий застосунок **Meta Horizon**.
2. **Devices** → твій Quest 3 → **Headset settings** → **Developer Mode** → увімкни.
3. Перезавантаж окуляр.

Назви пунктів у застосунку можуть трохи відрізнятися. Якщо щось не знаходиш, напиши мені, що саме бачиш.

### 1.4 Кабель і дозвіл

1. Потрібен **USB-C кабель, який передає дані**. Кабель «тільки для зарядки» не підійде, і це найчастіша причина, чому окуляр не видно.
2. Підключи Quest до Mac.
3. Одягни окуляр. З'явиться вікно «Allow USB debugging?». Постав галочку «Always allow from this computer» і натисни **Allow**.
4. Перевір у Terminal:

```bash
~/Library/Android/sdk/platform-tools/adb devices
```

У списку має бути рядок із серійним номером і словом **`device`**. Якщо там `unauthorized`, повтори пункт 3. Якщо список порожній, перевір кабель.

### 1.5 (Необов'язково) Meta VR Developer Hub

Програма для Mac, раніше називалась Meta Quest Developer Hub. Показує окуляр, логи й дозволяє ставити `.apk` мишею. Завантаження: <https://developers.meta.com/vr/downloads/package/oculus-developer-hub-mac/>. Для збірки з Unity вона не обов'язкова.

---

## Частина 2. Підготовка проєкту (більшість зроблю я, ти натискаєш кнопки в Unity)

Зараз проєкт використовує звичайний (Built-in) рендер і керування клавіатурою. Для Quest Meta радить таке:

1. **URP** (Universal Render Pipeline). Мета в інструкції створює проєкт із шаблону «Universal 3D». Наш проєкт ще не на URP, тож я переведу його (15 матеріалів, це невелика робота).
2. **Package Manager**: **Window → Package Management → Package Manager** → **+** → **Add package by name** → `com.meta.xr.sdk.all` (Meta XR All-in-One SDK). Якщо Unity спитає про Meta XR feature set, відповідай **Yes**. Якщо попросить перезапуск, натисни **Restart Editor**.
3. **XR Plug-in Management**: **Edit → Project Settings → XR Plug-in Management** (за потреби натисни Install). Увімкни **OpenXR** і на вкладці **Standalone**, і на вкладці **Android** (іконка робота). Перевір, що увімкнена група **Meta XR**.
4. **Build Profiles**: **File → Build Profiles** → платформа **Meta Quest** → **Enable Platform** (або **Switch Platform**). Якщо Unity запропонує встановити `com.unity.xr.openxr`, натисни **Install**.
5. **Project Setup Tool**: **Window → Meta → Tools → Project Setup Tool**. На вкладках **Standalone** і **Android** натисни **Fix All** (і **Apply All** для рекомендованих). Потім повторюй, доки не лишиться обов'язкових проблем.
6. **Active Input Handling**: **Project Settings → Player → Other Settings → Active Input Handling** → **Input System Package (New)**. Режим **Both** не підходить: під час збірки Unity 6.6 сама попереджає, що на Android він не підтримується. Код гри вже переведений на нову систему введення (`Assets/Scripts/FarmInput.cs`).
7. VR-режим уже є: `VrPlayer.cs` відповідає за голову й джойстики, `VrHud.cs` — за текст перед очима. Він вмикається сам, коли гра запущена в окулярі, а на Mac лишається клавіатура й миша. Руки й захоплення предметів додамо пізніше.

Meta XR All-in-One SDK (пункт 2) для першого запуску не потрібен, достатньо OpenXR. Поставимо його, коли дійдемо до рук і кидання яблук.

---

## Частина 3. Збірка й запуск в окулярі

1. Підключи Quest кабелем, одягни окуляр і дозволь USB-налагодження (див. 1.4).
2. У Unity: **File → Build Profiles** → **Meta Quest**.
3. У налаштуваннях запуску обери свій Quest як пристрій. Сцена `FarmPrototype` уже в списку збірки.
4. Натисни **Build And Run**. На запит куди зберегти обери папку `Builds` (вона вже в `.gitignore`) і назви файл `FarmsteadVR`.
5. Дочекайся кінця збірки. Перша збірка найдовша, а наступні швидші. Точних цифр документація не дає, орієнтуйся на 10–30 хвилин вперше і кілька хвилин потім. **[не з документації]**
6. Гра запуститься в окулярі сама. Пізніше її можна знайти в **Library → Unknown Sources**.

### Без кнопки Build And Run

Якщо потрібно просто поставити готовий файл:

```bash
~/Library/Android/sdk/platform-tools/adb install -r Builds/FarmsteadVR.apk
```

---

## Частина 4. Тестування без окуляра

**Meta XR Simulator** запускає VR-гру прямо в Unity на Mac із симульованими руками. У редакторі є іконка на панелі або меню **Window → Meta**. Перед першим використанням його треба встановити окремо, інструкція: <https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/>.

Симулятор показує інший вигляд і швидкодію, ніж справжній Quest, тож перед кожним «готово» перевіряй гру ще й в окулярі.

---

## Частина 5. Якщо щось не працює

| Проблема | Що перевірити |
| --- | --- |
| Quest не видно в `adb devices` | Кабель має передавати дані. Режим розробника ввімкнений. Дозвіл «Allow USB debugging» натиснутий. Спробуй `adb kill-server` і `adb start-server`. |
| У списку `unauthorized` | Одягни окуляр і натисни Allow ще раз. Якщо вікна немає, вимкни й увімкни USB-налагодження в налаштуваннях розробника. |
| Помилка про відсутній Android Build Support | Частина 1.1. |
| Збірка проходить, у окулярі чорний екран | OpenXR увімкнений лише на вкладці Standalone. Увімкни його на вкладці **Android**. |
| Помилки Gradle або SDK | **Unity → Settings → External Tools**: поряд з Android SDK, NDK, JDK мають стояти галочки «Use embedded» (за замовчуванням). |
| Не знаєш, чому гра падає | `adb logcat -s Unity` виводить повідомлення Unity з окуляра. |

---

## Джерела

- Налаштування Unity для Quest (ручний варіант): <https://developers.meta.com/horizon/documentation/unity/unity-tutorial-hello-world-manual-steps/>
- Налаштування проєкту з Meta VR Developer Hub: <https://developers.meta.com/horizon/documentation/unity/unity-project-setup/>
- XR Plug-in Management для Meta: <https://developers.meta.com/vr/documentation/unity/unity-xr-plugin/>
- Meta XR Simulator: <https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/>
- Link працює лише на Windows: <https://developers.meta.com/vr/documentation/unity/unity-link/>
