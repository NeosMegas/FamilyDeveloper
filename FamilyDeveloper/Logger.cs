using System;
using System.Diagnostics;
using System.IO;

namespace SimplePluginLogger
{
    public class Logger
    {
        public string LogFilePath { get; }

        /// <summary>
        /// Создание экземпляра класса с заданным файлом лога
        /// </summary>
        public Logger()
        {
            LogFilePath = Path.GetTempFileName();
        }

        /// <summary>
        /// Создание экземпляра класса с заданным файлом лога
        /// </summary>
        public Logger(string filePath)
        {
            LogFilePath = filePath;
        }


        /// <summary>
        /// Запись сообщения лога во временный файл
        /// </summary>
        /// <param name="message">Сообщение</param>
        public void Log(string message)
        {
            using (StreamWriter sr = File.AppendText(LogFilePath))
                sr.WriteLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff ") + message);
        }

        /// <summary>
        /// Открыть временный файл (например, в блокноте)
        /// </summary>
        public void OpenLogFile() => Process.Start(LogFilePath);
    }
}
