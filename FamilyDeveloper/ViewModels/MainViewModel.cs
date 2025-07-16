using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
