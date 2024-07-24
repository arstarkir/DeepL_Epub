
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

        public TValue GetTValue()
        {
            return countryCode;
        }
        public override string ToString()
        {
            return m_displayText;
        }
    }
}
