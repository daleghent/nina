using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Model {
    public class TooltipDescriptionAttribute : DescriptionAttribute {
        public TooltipDescriptionAttribute(string descriptionLabel, string tooltipLabel) 
            : base(descriptionLabel) {
            this.TooltipLabelValue = tooltipLabel;
        }

        public virtual string TooltipLabel { get => TooltipLabelValue; }

        protected string TooltipLabelValue { get; set; }
    }
}
