using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AstrophotographyBuddy.ViewModel{
    class SequenceVM : BaseINPC {

        public SequenceVM() {
            AddSequenceCommand = new RelayCommand(addSequence);
        }
        

        private ObservableCollection<SequenceModel> _sequence;
        public ObservableCollection<SequenceModel> Sequence {
            get {
                if(_sequence == null) {
                    _sequence = new ObservableCollection<SequenceModel>();
                    _sequence.Add(new SequenceModel());
                }
                return _sequence;
            }
            set {
                _sequence = value;
                RaisePropertyChanged();
            }
        }

        public void addSequence(object o) {
            Sequence.Add(new SequenceModel());
        }

        private ICommand _addSequenceCommand;
        public ICommand AddSequenceCommand {
            get { return _addSequenceCommand; }
            set { _addSequenceCommand = value;  RaisePropertyChanged(); }
        }
    }
}
