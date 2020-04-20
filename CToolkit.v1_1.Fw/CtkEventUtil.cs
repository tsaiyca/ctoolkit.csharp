﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CToolkit.v1_1
{

    public class CtkEventUtil
    {

        /// <summary>
        /// 移除某個event裡的所有delegate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eh"></param>
        public static void RemoveEventHandlers<T>(EventHandler<T> eh) where T : EventArgs
        {
            //Delegate.RemoveAll(,)
            var handlers = eh.GetInvocationList().ToList();
            foreach (var hdl in handlers)
                eh -= (EventHandler<T>)hdl;
        }

        /// <summary>
        /// 移除某個event裡的所有delegate
        /// </summary>
        /// <param name="dlgt"></param>
        /// <param name="target"></param>
        /// <param name="flags"></param>
        public static void RemoveEventHandlers(Delegate dlgt, Object target
            , BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            //Delegate.RemoveAll(,)
            var theEvent = dlgt.Target.GetType().GetEvent(dlgt.Method.Name, flags);
            var handlers = dlgt.GetInvocationList().ToList();
            foreach (var hdl in handlers)
                if (hdl.Target == target)
                    RemoveSubscriberEvenIfItsPrivate(theEvent, dlgt.Target, hdl, flags);
        }




        /// <summary>
        /// 在 owningObject 中, 移除所有與 targetObj 有關 event 的 delegate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventOwning"></param>
        /// <param name="dlgtObj"></param>
        public static void RemoveEventHandlersFromOwningByTarget(Object eventOwning, Object dlgtObj
            , BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            if (eventOwning == null || dlgtObj == null) return;

            foreach (DelegateInfo eventFromOwningObject in GetDelegates(eventOwning, flags))
            {
                //Run過所有訂閱者
                foreach (Delegate subscriber in eventFromOwningObject.GetInvocationList())
                {
                    if (subscriber.Target == dlgtObj)
                    {
                        EventInfo theEvent = eventFromOwningObject.GetEventInfo(flags);
                        RemoveSubscriberEvenIfItsPrivate(theEvent, eventOwning, subscriber, flags);
                    }
                }
            }
        }




        /// <summary>
        /// 清除 owningObject 裡所有 event 的 delegate
        /// </summary>
        /// <param name="filterFunc"></param>
        /// <param name="owningObject"></param>
        /// <param name="flags"></param>
        public static void RemoveEventHandlersFromOwningByFilter(Object owningObject, Func<Delegate, bool> filterFunc,
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            if (owningObject == null) return;

            foreach (DelegateInfo eventFromOwningObject in GetDelegates(owningObject, flags))
            {
                foreach (Delegate subscriber in eventFromOwningObject.GetInvocationList())
                {
                    if (filterFunc(subscriber))
                    {
                        EventInfo theEvent = eventFromOwningObject.GetEventInfo(flags);
                        RemoveSubscriberEvenIfItsPrivate(theEvent, owningObject, subscriber, flags);
                    }
                }
            }
        }

        /// <summary>
        /// 取得owningObject裡所有宣告為Delegate的Field
        /// </summary>
        /// <param name="owningObject"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private static DelegateInfo[] GetDelegates(object owningObject, BindingFlags flags)
        {

            var delegates = new List<DelegateInfo>();
            var potentialEvetns = new List<FieldInfo>();

            var type = owningObject.GetType();
            do
            {
                potentialEvetns.AddRange(type.GetFields(flags));
                type = type.BaseType;
            } while (type != null);


            foreach (FieldInfo privateFieldInfo in potentialEvetns)
            {
                Delegate eventFromOwningObject = privateFieldInfo.GetValue(owningObject) as Delegate;
                //可以成功轉為Delegate的, 記錄下來
                if (eventFromOwningObject != null)
                {
                    delegates.Add(new DelegateInfo(eventFromOwningObject, privateFieldInfo, owningObject));
                }
            }

         


            return delegates.ToArray();
        }

        // You can use eventInfo.RemoveEventHandler(owningObject, subscriber) 
        // unless it's a private delegate
        private static void RemoveSubscriberEvenIfItsPrivate(
          EventInfo eventInfo, object owningObject, Delegate subscriber, BindingFlags flags)
        {
            MethodInfo privateRemoveMethod = eventInfo.GetRemoveMethod(true);
            privateRemoveMethod.Invoke(owningObject, flags, null, new object[] { subscriber }, CultureInfo.CurrentCulture);
        }
        private class DelegateInfo
        {
            public readonly Delegate delegateInformation;
            public readonly FieldInfo fieldInfo;
            public readonly object owningObject;

            public DelegateInfo(Delegate delegateInformation, FieldInfo fieldInfo, object owningObject)
            {
                this.delegateInformation = delegateInformation;
                this.fieldInfo = fieldInfo;
                this.owningObject = owningObject;
            }

            public EventInfo GetEventInfo(BindingFlags flags)
            {
                return owningObject.GetType().GetEvent(fieldInfo.Name, flags);
            }

            public Delegate[] GetInvocationList()
            {
                return delegateInformation.GetInvocationList();
            }
        }
    }
}
