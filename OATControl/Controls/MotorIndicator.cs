using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OATControl.Controls
{
	public class MotorIndicator : FrameworkElement
	{
		static MotorIndicator()
		{
			IsEnabledProperty.OverrideMetadata(typeof(MotorIndicator), new FrameworkPropertyMetadata(true, SomePropertyChanged));
		}

		public static readonly DependencyProperty MotorNameProperty = DependencyProperty.Register(
				"MotorName",
				typeof(string),
				typeof(MotorIndicator),
				new PropertyMetadata("", MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
				"IsRunning",
				typeof(bool),
				typeof(MotorIndicator),
				new PropertyMetadata(false, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
				"Background",
				typeof(Brush),
				typeof(MotorIndicator),
				new PropertyMetadata(Brushes.White, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ActiveBackgroundProperty = DependencyProperty.Register(
				"ActiveBackground",
				typeof(Brush),
				typeof(MotorIndicator),
				new PropertyMetadata(Brushes.White, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(MotorIndicator),
				new PropertyMetadata(Brushes.Black, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ActiveForegroundProperty = DependencyProperty.Register(
				"ActiveForeground",
				typeof(Brush),
				typeof(MotorIndicator),
				new PropertyMetadata(Brushes.Black, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty DisabledForegroundProperty = DependencyProperty.Register(
				"DisabledForeground",
				typeof(Brush),
				typeof(MotorIndicator),
				new PropertyMetadata(Brushes.Gray, MotorIndicator.SomePropertyChanged));

		private static void SomePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			((MotorIndicator)obj).InvalidateVisual();
		}

		public string MotorName
		{
			get
			{
				return (string)this.GetValue(MotorIndicator.MotorNameProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.MotorNameProperty, value);
			}
		}

		public bool IsRunning
		{
			get
			{
				return (bool)this.GetValue(MotorIndicator.IsRunningProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.IsRunningProperty, value);
			}
		}

		public Brush Foreground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ForegroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ForegroundProperty, value);
			}
		}

		public Brush ActiveForeground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ActiveForegroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ActiveForegroundProperty, value);
			}
		}

		public Brush Background
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.BackgroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.BackgroundProperty, value);
			}
		}

		public Brush ActiveBackground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ActiveBackgroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ActiveBackgroundProperty, value);
			}
		}

		public Brush DisabledForeground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.DisabledForegroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.DisabledForegroundProperty, value);
			}
		}

		protected override void OnRender(DrawingContext dc)
		{
			double rectSize = RenderSize.Height;

			Point p1 = new Point(0.0 * rectSize, 0.00 * rectSize);
			Point p2 = new Point(1.0 * rectSize, 0.00 * rectSize);
			Point p3 = new Point(1.0 * rectSize, 1.00 * rectSize);
			Point p4 = new Point(0.0 * rectSize, 1.00 * rectSize);

			var bg = IsRunning ? ActiveBackground : Background;
			var borderBrush = IsRunning ? ActiveForeground : Background;

			dc.DrawRectangle(bg, null, new Rect(p1, new Size(rectSize, rectSize)));
			dc.DrawRectangle(null, new Pen(borderBrush, 1.0), new Rect(new Point(0.5, 0.5), new Size(rectSize - 1.0, rectSize - 1.0)));

			var formatText = new FormattedText(
				MotorName,
				CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface("Verdana"),
				10,
				!IsEnabled ? DisabledForeground : IsRunning ? ActiveForeground : Foreground,
				1.0);

			dc.DrawText(formatText, new Point(rectSize + 2, -2.5));
		}
	}
}
