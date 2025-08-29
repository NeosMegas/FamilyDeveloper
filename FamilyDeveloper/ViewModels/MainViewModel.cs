using Autodesk.Revit.DB;
using FamilyDeveloper.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FamilyDeveloper.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string FamilyVersion { get => model.GetFamilyVersion(); }
        private FamilyVersionModel model;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public MainViewModel(Document doc)
        {
            model = new FamilyVersionModel(doc);
        }

    }
}
