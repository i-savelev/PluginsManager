using Autodesk.Revit.UI;
using System;
using System.Diagnostics;

namespace PluginsManager
{
    public class Handler : IExternalEventHandler
    {
        private CommandManager _command_manager;

        public Handler(CommandManager command_manager)
        {
            _command_manager = command_manager;
        }
        public void Execute(UIApplication app)
        {
            var commandName = GlobComandName.Name;
            var stopwatch = Stopwatch.StartNew();
            Logger.Info($"Начало выполнения команды [{commandName}]");

            try
            {
                _command_manager.RunCommand(commandName);
                stopwatch.Stop();
                Logger.Info($"Команда [{commandName}] завершена за {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Exception(ex, $"Ошибка выполнения команды [{commandName}] через {stopwatch.ElapsedMilliseconds} ms");
                TaskDialog td = new TaskDialog("Ошибка");
                td.MainContent = $"{ex.Message}\n\n[Подробности]\n{ex.GetBaseException()}";
                td.Show();
            }
            finally
            {
                Logger.Debug("Освобождение ExternalEvent");
                _command_manager.ExternalEvent?.Dispose();
                _command_manager.ExternalEvent = null;
            }
        }

        public string GetName()
        {
            return "My Dynamic External Event Handler";
        }
    }
}
