﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CToolkit.v1_0
{
    public class CtkUtil
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        public static bool EnableUnsafeHeaderParsing()
        {
            //Get the assembly that contains the internal class
            Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
            if (aNetAssembly != null)
            {
                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (aSettingsType != null)
                {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created allready the property will create it for us.
                    object anInstance = aSettingsType.InvokeMember("Section",
                      BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });

                    if (anInstance != null)
                    {
                        //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                        FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null)
                        {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, true);
                            return true;
                        }
                    }
                }
            }
            return false;
        }



        public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
        {
            var body = memberAccess.Body;
            var member = body as MemberExpression;
            if (member != null) return member.Member.Name;

            var unary = body as UnaryExpression;
            if (unary != null)
            {
                if (unary.Method != null) return unary.Method.Name;
            }
            throw new ArgumentException();

        }

        public static string GetMethodName<T>(Expression<Func<T, Delegate>> expression)
        {
            var unaryExpression = (UnaryExpression)expression.Body;
            var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;

            var IsNET45 = Type.GetType("System.Reflection.ReflectionContext", false) != null;
            if (IsNET45)
            {
                var methodCallObject = (ConstantExpression)methodCallExpression.Object;
                var methodInfo = (MethodInfo)methodCallObject.Value;
                return methodInfo.Name;
            }
            else
            {
                var methodInfoExpression = (ConstantExpression)methodCallExpression.Arguments.Last();
                var methodInfo = (MemberInfo)methodInfoExpression.Value;
                return methodInfo.Name;
            }
        }

        public static T LoadXmlOrNew<T>(String fn) where T : class, new()
        {
            var seri = new System.Xml.Serialization.XmlSerializer(typeof(T));
            var fi = new FileInfo(fn);
            if (!fi.Exists)
            {
                var config = new T();
                return config;
            }


            using (var stm = fi.OpenRead())
            {
                return seri.Deserialize(stm) as T;
            }
        }

        public static T LoadXml<T>(String fn)
        {
            var seri = new System.Xml.Serialization.XmlSerializer(typeof(T));
            var fi = new FileInfo(fn);
            if (!fi.Exists) return default(T);


            using (var stm = fi.OpenRead())
            {
                return (T)seri.Deserialize(stm);
            }
        }





        public static T ParseEnum<T>(String val) { return (T)Enum.Parse(typeof(T), val); }

        public static void SaveToXmlFile(object obj, String fn)
        {
            var seri = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            var fi = new FileInfo(fn);

            if (!fi.Directory.Exists) fi.Directory.Create();

            using (var stm = fi.Open(FileMode.Create))
            {
                seri.Serialize(stm, obj);
            }
        }

        public static void SaveToXmlFileT<T>(T obj, String fn)
        {
            var seri = new System.Xml.Serialization.XmlSerializer(typeof(T));
            var fi = new FileInfo(fn);

            if (!fi.Directory.Exists) fi.Directory.Create();

            using (var stm = fi.Open(FileMode.Create))
            {
                seri.Serialize(stm, obj);
            }
        }
        public static void SaveToXmlFile(Type type, object obj, String fn)
        {
            var seri = new System.Xml.Serialization.XmlSerializer(type);
            var fi = new FileInfo(fn);

            if (!fi.Directory.Exists) fi.Directory.Create();

            using (var stm = fi.Open(FileMode.Create))
            {
                seri.Serialize(stm, obj);
            }
        }


        public static object TryCatch(Action theMethod, params object[] parameters)
        {
            try
            {
                return theMethod.DynamicInvoke(parameters);
            }
            catch (Exception ex)
            {
                CtkLog.Write(ex);
                return ex;
            }
        }




        public static int RandomInt()
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            var cnt = rnd.Next(32);
            for (var idx = 0; idx < cnt; idx++) rnd.Next();

            return rnd.Next();
        }
        public static int RandomInt(int max)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            var cnt = rnd.Next(32);
            for (var idx = 0; idx < cnt; idx++) rnd.Next();

            return rnd.Next(max);
        }
        public static int RandomInt(int min, int max)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            var cnt = rnd.Next(32);
            for (var idx = 0; idx < cnt; idx++) rnd.Next();

            return rnd.Next(min, max);
        }


    }




}
