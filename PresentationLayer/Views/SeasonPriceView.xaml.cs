using System.Linq;
using EntityLayer;
using DataLayer.Interfaces;
using System.Collections.Generic;
using System.Windows;

namespace PresentationLayer.Views
{
    public partial class SeasonPriceView : Window
    {
        private List<SeasonPrice> seasonPrices;
        private readonly IUnitOfWork _unitOfWork;

        public SeasonPriceView(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            InitializeComponent();

            seasonPrices = _unitOfWork.SeasonPrice.GetAll().ToList();


            seasonPriceDataGrid.ItemsSource = seasonPrices;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            seasonPriceDataGrid.CommitEdit();
            seasonPriceDataGrid.CommitEdit();

            foreach (SeasonPrice seasonPrice in seasonPrices)
            {
                if (seasonPrice.Price <= 0)
                {
                    MessageBox.Show("Price cannot be empty, zero or negative.");
                    return;
                }
            }

            foreach (SeasonPrice seasonPrice in seasonPrices)
            {
                _unitOfWork.SeasonPrice.Update(seasonPrice);
            }

            _unitOfWork.Complete();

            MessageBox.Show("Prices saved successfully.");
        }
    }
}