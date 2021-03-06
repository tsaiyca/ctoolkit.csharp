﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CToolkit.v1_1.Config
{
    public abstract class CtkConfigBase
    {

        public void SaveXml(string fn) { CtkUtilFw.SaveToXmlFile(this, fn); }

        public static T LoadXml<T>(string fn) where T : class, new() { return CtkUtilFw.LoadXmlOrNew<T>(fn); }


    }
}
