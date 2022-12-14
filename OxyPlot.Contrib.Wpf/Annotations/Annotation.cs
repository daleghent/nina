// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Annotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   The annotation base class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Annotations;
    using OxyPlot.Contrib.Wpf;

    /// <summary>
    /// The annotation base class.
    /// </summary>
    public abstract class Annotation : FrameworkElement
    {
        static Annotation()
        {
            WidthProperty.OverrideMetadata(typeof(Annotation), new FrameworkPropertyMetadata(double.NaN, AppearanceChanged));
            HeightProperty.OverrideMetadata(typeof(Annotation), new FrameworkPropertyMetadata(double.NaN, AppearanceChanged));
        }

        /// <summary>
        /// Identifies the <see cref="Layer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register(
            "Layer",
            typeof(AnnotationLayer),
            typeof(Annotation),
            new PropertyMetadata(AnnotationLayer.AboveSeries, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="XAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisKeyProperty = DependencyProperty.Register(
            "XAxisKey",
            typeof(string),
            typeof(Annotation),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="YAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisKeyProperty = DependencyProperty.Register(
            "YAxisKey",
            typeof(string),
            typeof(Annotation),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipByXAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClipByXAxisProperty = DependencyProperty.Register(
            "ClipByXAxis",
            typeof(bool),
            typeof(Annotation),
            new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipByYAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClipByYAxisProperty = DependencyProperty.Register(
            "ClipByYAxis",
            typeof(bool),
            typeof(Annotation),
            new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="EdgeRenderingMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EdgeRenderingModeProperty = DependencyProperty.Register(
            "EdgeRenderingMode",
            typeof(EdgeRenderingMode),
            typeof(Annotation),
            new PropertyMetadata(EdgeRenderingMode.Automatic, AppearanceChanged));

        /// <summary>
        /// Gets or sets the rendering layer of the annotation. The default value is <see cref="AnnotationLayer.AboveSeries" />.
        /// </summary>
        public AnnotationLayer Layer
        {
            get
            {
                return (AnnotationLayer)this.GetValue(LayerProperty);
            }

            set
            {
                this.SetValue(LayerProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the X axis key.
        /// </summary>
        public string XAxisKey
        {
            get
            {
                return (string)this.GetValue(XAxisKeyProperty);
            }

            set
            {
                this.SetValue(XAxisKeyProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the Y axis key.
        /// </summary>
        public string YAxisKey
        {
            get
            {
                return (string)this.GetValue(YAxisKeyProperty);
            }

            set
            {
                this.SetValue(YAxisKeyProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the annotation line by the X axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by the X axis is enabled; otherwise, <c>false</c>.</value>
        public bool ClipByXAxis
        {
            get
            {
                return (bool)this.GetValue(ClipByXAxisProperty);
            }

            set
            {
                this.SetValue(ClipByXAxisProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the annotation line by the Y axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by the Y axis is enabled; otherwise, <c>false</c>.</value>
        public bool ClipByYAxis
        {
            get
            {
                return (bool)this.GetValue(ClipByYAxisProperty);
            }

            set
            {
                this.SetValue(ClipByYAxisProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="OxyPlot.EdgeRenderingMode"/> for the annotation.
        /// </summary>
        public EdgeRenderingMode EdgeRenderingMode
        {
            get
            {
                return (EdgeRenderingMode)this.GetValue(EdgeRenderingModeProperty);
            }

            set
            {
                this.SetValue(EdgeRenderingModeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the internal annotation object.
        /// </summary>
        public Annotations.Annotation InternalAnnotation { get; protected set; }

        /// <summary>
        /// Creates the internal annotation object.
        /// </summary>
        /// <returns>The annotation.</returns>
        public abstract Annotations.Annotation CreateModel();

        /// <summary>
        /// Synchronizes the properties.
        /// </summary>
        public virtual void SynchronizeProperties()
        {
            var a = this.InternalAnnotation;
            a.Layer = this.Layer;
            a.XAxisKey = this.XAxisKey;
            a.YAxisKey = this.YAxisKey;
            a.ClipByXAxis = this.ClipByXAxis;
            a.ClipByYAxis = this.ClipByYAxis;
            a.EdgeRenderingMode = this.EdgeRenderingMode;
            a.ToolTip = this.ToolTip as string;
        }

        /// <summary>
        /// Handles changes in appearance.
        /// </summary>
        /// <param name="d">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        protected static void AppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (((Annotation)d).Parent as IPlot)?.ElementAppearanceChanged(d);
        }

        /// <summary>
        /// Handles changes in data.
        /// </summary>
        /// <param name="d">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        protected static void DataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (((Annotation)d).Parent as IPlot)?.ElementDataChanged(d);
        }
    }
}
