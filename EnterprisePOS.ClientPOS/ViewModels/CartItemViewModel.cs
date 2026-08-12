using System;
using System.ComponentModel;

namespace EnterprisePOS.ClientPOS.ViewModels
{
    public class CartItemViewModel : INotifyPropertyChanged
    {
        private int _quantity;

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(SubTotal));
                }
            }
        }

        public decimal SubTotal => Quantity * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
