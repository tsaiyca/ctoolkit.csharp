﻿using CToolkit.v1_0.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CToolkit.v1_0
{
    public class CtkLog
    {
        public static string LoggerAssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }
        public static CtkLogger DefaultLogger { get { return CtkLoggerMapper.Singleton.Get(); } }
        public static CtkLogger ThisLogger { get { return GetAssemblyLogger(typeof(CtkLog)); } }

        public static void Write(CtkLoggerEventArgs ea)
        {
            ThisLogger.Write(ea);
        }
        public static void Write(CtkLoggerEventArgs ea, CtkLoggerEnumLevel _level)
        {
            ea.Level = _level;
            ThisLogger.Write(ea);
        }
        //public static void Write(string msg, params object[] args) { Logger.Write(string.Format(msg, args)); }會造成呼叫模擬兩可

        public static void Verbose(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Verbose); }
        public static void Debug(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Debug); }
        /// <summary>
        /// 使用 空ID 記錄Log
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Info(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Info); }
        public static void Warn(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Warn); }
        public static void Error(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Error); }
        public static void Fatal(string msg, params object[] args) { ThisLogger.Write(string.Format(msg, args), CtkLoggerEnumLevel.Fatal); }

        public static void VerboseNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Verbose); }
        public static void DebugNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Debug); }
        /// <summary>
        /// 使用 Namespace 記錄Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void InfoNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Info); }
        public static void WarnNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Warn); }
        public static void ErrorNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Error); }
        public static void FatalNs(object sender, string msg, params object[] args) { GetAssemblyLogger(sender).Write(string.Format(msg, args), CtkLoggerEnumLevel.Fatal); }


        public static void VerboseId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Verbose); }
        public static void DebugId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Debug); }
        /// <summary>
        /// 使用 指定ID 記錄Log
        /// </summary>
        /// <param name="loggerId"></param>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void InfoId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Info); }
        public static void WarnId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Warn); }
        public static void ErrorId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Error); }
        public static void FatalId(string loggerId, string msg, params object[] args) { GetLoggerById(loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Fatal); }


        public static void VerboseNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Verbose); }
        public static void DebugNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Debug); }
        /// <summary>
        /// 使用 Namespace + 指定ID 記錄Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="loggerId"></param>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void InfoNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Info); }
        public static void WarnNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Warn); }
        public static void ErrorNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Error); }
        public static void FatalNsId(object sender, string loggerId, string msg, params object[] args) { GetAssemblyLoggerById(sender, loggerId).Write(string.Format(msg, args), CtkLoggerEnumLevel.Fatal); }



        public static CtkLogger GetLoggerById(string loggerId) { return CtkLoggerMapper.Singleton.Get(loggerId); }
        public static CtkLogger GetAssemblyLogger(Object sender)
        {
            var type = sender.GetType();
            if (sender is Type)
                type = sender as Type;

            var name = type.Assembly.FullName;
            return CtkLoggerMapper.Singleton.Get(name);
        }
        public static CtkLogger GetAssemblyLoggerById(Object sender, string loggerId)
        {
            var type = sender.GetType();
            var name = type.Assembly.FullName + (string.IsNullOrEmpty(loggerId) ? "" : "." + loggerId);
            return CtkLoggerMapper.Singleton.Get(name);
        }



        public static void RegisterAllLogger(EventHandler<CtkLoggerEventArgs> evt, Func<string, bool> filter = null)
        {
            if (filter == null) filter = (name) => true;
            CtkLoggerMapper.Singleton.RegisterAllLogger(evt, filter);
        }


    }
}