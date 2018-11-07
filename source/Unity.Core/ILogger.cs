using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Core
{
    public interface ILogger
    {
        void Error(string message);
        void Info(string message);
        void Debug(string message);
    }

    public class DefaultLogger : ILogger
    {
        static DefaultLogger s_Instance = new DefaultLogger();

        public static ILogger Instance => s_Instance;

        void ILogger.Error(string message) => Console.Error.WriteLine(message);
        void ILogger.Info(string message) => Console.WriteLine(message);
        void ILogger.Debug(string message) => Debug.WriteLine(message);
    }

    public class StringLogger : ILogger
    {
        List<string> m_Errors = new List<string>();
        List<string> m_Infos = new List<string>();
        List<string> m_Debugs = new List<string>();

        void ILogger.Error(string message) => m_Errors.Add(message);
        void ILogger.Info(string message) => m_Infos.Add(message);
        void ILogger.Debug(string message) => m_Debugs.Add(message);

        public IReadOnlyCollection<string> Errors => m_Errors;
        public IReadOnlyCollection<string> Infos => m_Infos;
        public IReadOnlyCollection<string> Debugs => m_Debugs;

        public string ErrorsAsString => m_Errors.StringJoin('\n');
        public string InfosAsString => m_Infos.StringJoin('\n');
        public string DebugsAsString => m_Debugs.StringJoin('\n');
    }
}
