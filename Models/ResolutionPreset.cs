using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RRes.Models
{
    public class ResolutionPreset : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string DisplayName { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsNative { get; set; }
        public string Category { get; set; } = string.Empty;
        public string AspectRatio { get; set; } = "4:3";
        public string RatioBadgeColor { get; set; } = "#2C3240";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ResolutionText => $"{Width} × {Height}";

        public override string ToString() => DisplayName;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

