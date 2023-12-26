namespace CustomLibrary.Buttons.Interfaces {
    public interface IDoubleClickIndependent {
        // Check if is double click
        public bool EnableClick { get; set; }
        public int ClickTimes { get; set; }
        public int Milliseconds { get; set; }
        public System.Timers.Timer ClickTimer { get; set; }
    }
}
