using System.ComponentModel;
using System.Runtime.CompilerServices;
using mprNestedFamilyParamsTransfer.Annotations;

namespace mprNestedFamilyParamsTransfer.Helpers
{
    public class VmBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
