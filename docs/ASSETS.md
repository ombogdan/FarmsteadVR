# Журнал ресурсів (моделі, звуки, музика, шрифти)

Тут записано **кожен** файл, який потрапляє в гру, і звідки він узявся. Це захищає від проблем з авторськими правами: якщо колись спитають, «а це звідки?», відповідь буде тут.

## Правила

1. Беремо тільки те, ліцензію чого розуміємо. **Немає ліцензії — не беремо.**
2. Ніяких персонажів, логотипів і моделей з чужих ігор, фільмів і мультфільмів, навіть «схожих».
3. Ліцензії:
   - **CC0** — можна все, автора вказувати не обов'язково. Найкращий варіант.
   - **CC-BY** — можна, але автор має бути в титрах гри.
   - **NC (Non-Commercial)** — **не беремо**, бо гру, можливо, продаватимемо.
   - **Editorial** — для ігор не підходить.
   - **Unity Asset Store** — у своїй грі можна, перепродавати сам набір не можна.
4. Після кожного завантаження додаємо рядок у таблицю нижче. Сторінку з ліцензією бажано зберегти як скриншот.

## Що вже в грі

| Що | Де лежить | Автор / джерело | Ліцензія | Дата |
| --- | --- | --- | --- | --- |
| Усі звуки: вітер, пташки, стук, «чпок», монетка, фанфари, «вжух», «тік», «кукуріку», двигун і гудок автобуса, «пш-ш» дверей, гортання сторінки (голос Бориса прибрали) | `Assets/Resources/Sounds/*.wav` | Згенеровано нашим скриптом `Tools/SoundGen/generate_sounds.py` з математики (синусоїди, шум, фільтри), без записів і чужих семплів | Наші власні | 2026-11-20 |
| Матеріали (кольори) | `Assets/Resources/FarmMaterials/*.mat` | Створені скриптом `Assets/Editor/FarmSceneInitializer.cs` | Наші власні | 2026-11-20 |
| Моделі (будинки, Борис, ящик, паркан, дерева) | Будуються кодом у `FarmPrototype.cs` з простих фігур Unity | Наші власні | Наші власні | 2026-11-20 |
| Шрифт тексту у VR | Вбудований шрифт Unity (`LegacyRuntime.ttf`) | Unity | Входить в Unity. **Перед релізом перевірити ліцензію** або замінити на шрифт з ліцензією OFL (наприклад, з Google Fonts) | 2026-11-20 |

| Луг | `Assets/Resources/CountryArt/` | [Poly Haven · leafy_grass](https://polyhaven.com/a/leafy_grass) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Стежка | `Assets/Resources/CountryArt/` | [Poly Haven · grass_path_3](https://polyhaven.com/a/grass_path_3) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Обшивка | `Assets/Resources/CountryArt/` | [Poly Haven · weathered_plank_siding](https://polyhaven.com/a/weathered_plank_siding) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Черепиця | `Assets/Resources/CountryArt/` | [Poly Haven · clay_roof_tiles_02](https://polyhaven.com/a/clay_roof_tiles_02) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Штукатурка | `Assets/Resources/CountryArt/` | [Poly Haven · worn_cracked_plaster](https://polyhaven.com/a/worn_cracked_plaster) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Деревина | `Assets/Resources/CountryArt/` | [Poly Haven · weathered_brown_planks](https://polyhaven.com/a/weathered_brown_planks) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Кора | `Assets/Resources/CountryArt/` | [Poly Haven · bark_brown_02](https://polyhaven.com/a/bark_brown_02) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Асфальт | `Assets/Resources/CountryArt/` | [Poly Haven · asphalt_04](https://polyhaven.com/a/asphalt_04) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Хвоя | `Assets/Resources/CountryArt/` | [Poly Haven · fir_tree_01](https://polyhaven.com/a/fir_tree_01) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Листя | `Assets/Resources/CountryArt/` | [Poly Haven · tree_small_02](https://polyhaven.com/a/tree_small_02) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Бочка | `Assets/Resources/CountryArt/` | [Poly Haven · wine_barrel_01](https://polyhaven.com/a/wine_barrel_01) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Польові квіти | `Assets/Resources/CountryArt/` | [Poly Haven · flower_gazania](https://polyhaven.com/a/flower_gazania) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Трава | `Assets/Resources/CountryArt/` | [Poly Haven · grass_medium_01](https://polyhaven.com/a/grass_medium_01) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Кущі | `Assets/Resources/CountryArt/` | [Poly Haven · shrub_02](https://polyhaven.com/a/shrub_02) | [CC0](https://polyhaven.com/license) | 2026-11-20 |
| Панорама неба | `Assets/Resources/CountryArt/` | [Poly Haven · kloofendal_48d_partly_cloudy](https://polyhaven.com/a/kloofendal_48d_partly_cloudy) | [CC0](https://polyhaven.com/license) | 2026-11-20 |

Силуети дальнього лісу запечені у Unity з моделей сцени. Геометрія соняшників, пагорбів та модель Бориса створені для FarmsteadVR.

| Фото з листа діда | `Assets/Resources/ArrivalArt/FamilyPhoto.jpg` | Створено в ImageGen для FarmsteadVR | Створено для проєкту | 2026-11-20 |
| Курочки, півень, автобус і реквізит вступу | `Assets/Resources/ArrivalArt/` | Оригінальні 3D-моделі, текстури оперення, тканини й кузова | Створено для проєкту | 2026-11-20 |

## Як перегенерувати звуки

```bash
python3 Tools/SoundGen/generate_sounds.py
```

Щоб замінити звук кращим, поклади новий файл з **тією ж назвою** в `Assets/Resources/Sounds/` і додай рядок у таблицю.
