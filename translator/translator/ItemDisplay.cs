using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace translator
{
    public class ItemDisplay<TValue>
    {
        private readonly string m_displayText;
        private TValue countryCode { get; set; }

        public ItemDisplay(TValue countryCode, String displayCountry)
        {
            this.countryCode = countryCode;
            m_displayText = displayCountry;
        }

        public string GetCountryCode()
        {
            return typeof(TValue) == typeof(string) ? countryCode.ToString() : null;
        }
        public override string ToString()
        {
            return m_displayText;
        }
    }
}
