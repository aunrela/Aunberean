using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Service.Lib.Settings;

namespace Aunberean
{
    public class KtSetting<T> : Setting<T>
    {
        public KtSetting(T initialValue) : base(initialValue)
        {
            SettingType = (SettingType)30;
        }
    }
}
