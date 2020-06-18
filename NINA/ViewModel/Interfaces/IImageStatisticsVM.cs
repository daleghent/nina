using NINA.Model.ImageData;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    public interface IImageStatisticsVM : IDockableVM {
        AllImageStatistics Statistics { get; set; }

        Task UpdateStatistics(IImageData imageData);
    }
}