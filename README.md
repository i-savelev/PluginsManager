# Plugins Manager

<img src="https://github.com/user-attachments/assets/8320543f-1935-46c6-8867-d785762872fa" width="600" />


Данный инструмент решает проблему доставки обновлений и новых плагинов до пользователей.

PluginsManager - плагин для Revit, который позволяет создавать гибкое настраиваемое пространство для запуска команд из других плагинов. Данный инструмент вдохновлен проектом Revit Add-in Manager.

Плагин подгружает специально настроенные команды из всех dll файлов в выбранной папке и создает структуру из вкладок в отдельном окне. С PluginsManager становится достаточным просто обновить dll файл в папке на общем сервере, а для пользователей обновления становятся полностью бесшовными.


# Features

## Установка

Плагин устанавливается с помощью Plugins Manager.msi. На данный момент поддерживаются версии Revit21-24

## Запуск

Плагин рсполагается на вкладке "Надстройки" и при первом запуске предлагает выбрать папку с dll файлами.

![image](https://github.com/user-attachments/assets/b72225a7-1f5a-44ab-9eed-f0da4482376a)

После выбора папки и при последующих запусках будет появляться основное окно плагина

![image](https://github.com/user-attachments/assets/49db3be0-a7ff-4cb7-a61d-f9bd4f5b3a2d)

1. Кнопка для выбора папки с dll файлами
2. Вкладки. Гибко и быстра настраиваются (см. п. "Настройка и дополнительные возможности")
3. Таблица с командами выбранной вкладки
4. Кнопки для запуска команд
5. Информация о плагине, которая указана в `IS_DESCRIPTION` или в `<CmdDescription></CmdDescription>` (см. п. "Настройка и дополнительные возможности")

## Подготовка внешних dll файлов
Одним из вариантов пападания команды в окно плагина является добавление специальных полей прямо в класс с интерфейсом IExternalCommand:

```c#
public static string IS_TAB_NAME => "Название вкладки";
public static string IS_NAME => "Название команды";
public static string IS_IMAGE => "Название проекта.Resources.Название изображения.png";
public static string IS_DESCRIPTION => "Описание";
```
Для того, чтобы изображение считалось плагином, оно должно быть добавлено в ресурсы проекта и иметь свойство "Embedded resource"

## Настройка и дополнительные возможности

### Настройки папки внешних команд

Плагину доступны все dll файлы внтури указанной папки и все dll файлы в вложенных папках первого уровня.

При первом обращении к указанной папке внтури будет создан файл "config.xml" и папка "img". В файле "config.xml" можно указывать полное имя команды из dll, влкадку, отображаемое имя, описание, название картинки из папки "img".

```xml
<Commands>
  <Command>
    <CmdCode>Имя проекта.Имя класса</CmdCode>
    <CmdTab>Название вкладки</CmdTab>
    <CmdName>Имя для отображения</CmdName>
    <CmdDescription>Описание команды</CmdDescription>
    <CmdImage>image.png</CmdImage>
  </Command>
</Commands>
```
Таким образом можно добавлять команды из любых dll без необходимости вносить изменения в исхоный код.

Настрйоки, указанные в классе команды переопределяются настройкми из файла "config.xml".

Для получения всех доступных команд из dll файлов в текуще папке можно воспользоваться функцией "Показать все команды в папке" на панели Plugins Manager. 

![image](https://github.com/user-attachments/assets/bff45445-0276-4e78-92bc-c39475f4e17d)

При нажатии появится окно с таблицей полных имен всех команд. Эти имена нужно указывать в поле `<CmdCode></CmdCode>`

![image](https://github.com/user-attachments/assets/a40f9cf0-f1bb-41d8-a65a-b15daae7591a)


### Настройки пользователя

Плагин создает файл конфигурации в директории %appdata%/PluginsManager при первом запуске.

```xml
<Settings>
  <FolderPath>C:\Users\ilyas\Desktop\SVLV\temp\savelev_plugin\savelev_plugin\bin\Debug</FolderPath>
  <ExceptionTabs>Здесь можно указать названия вкладок, которые нужно скрыть</ExceptionTabs>
  <Post>user</Post>
</Settings>
```
FolderPath - сюда сохраняется путь к папке с dll файлами

ExceptionTabs - тут можно через любой разделитель указать названия вкладок, которые нужно скрыть. Например, таким образом можно для архитекторов скрыть вкладки "КР" и "ИОС". По умолчанию поле пустое.

Post - поле для обозначения роли. По умолчанию "user". Если значение заменить на "manager", то станут доступны вкладки, у которых в названии есть решетка "#". Таким образом можно отделять команды и плагины для тестирвоания или для внутреннего использования.

