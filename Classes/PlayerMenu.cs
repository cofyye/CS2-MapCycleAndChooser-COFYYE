namespace Classes
{
    public class PlayerMenu
    {
        public int CurrentIndex { get; set; }
        public bool ButtonPressed { get; set; }
        public bool MenuOpened { get; set; }
        public bool ForceMenuOpened { get; set; }
        public required string Html { get; set; }
    }
}
