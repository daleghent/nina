using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NINACustomControlLibrary {

    public class DetachingExpander : Expander {
        public object _content;

        protected override void OnExpanded() {
            if (_content != null)
                Content = _content;
            base.OnExpanded();
        }

        protected override void OnCollapsed() {
            _content = Content;
            Content = null;
            base.OnCollapsed();
        }
    }
}