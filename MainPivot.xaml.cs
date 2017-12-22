using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace Resolver
{
	public partial class MainPivot : PhoneApplicationPage
	{
		private readonly BackgroundWorker _thread = new BackgroundWorker();
		private readonly StringBuilder _html = new StringBuilder();
		private const string _header = "<html><body><div style='font-size:45;width:3000px;'>";
		private const string _footer = "</div></body></html>";
		private bool _threadRunning;

		public MainPivot()
		{
			InitializeComponent();

			Deployment_SelectionChanged(this, null);
			Repeat_Click(this, null);
			ClearBrowser();
			InitBackgroundThreadEvents();

			// set resolver credentials
			Resolver.UserName = "resolver.client";
			Resolver.SecurityKey = "xWiNDOvoTnI1oSwUeVH7uMs7KMyfIlfS";
		}

		private async void Test_Click(object sender, RoutedEventArgs e)
		{
			if (_threadRunning)
			{
				SetTestButtonText("Stopping Test...");
				_thread.CancelAsync();
				return;
			}

			ClearBrowser();
			Pivot.SelectedIndex = 0;

			var data = new ResolverData()
			{
				Host = Cell.SelectedItem.ToString(),
				Method = Method.SelectedItem.ToString(),
				Cell = Cell.SelectedItem.ToString(),
				Payload = Payload.Text,
				Type = Type.Text,
				ClientData = ClientData.Text,
				GetCell = GetCell.IsChecked ?? false,
				RepeatDelay = Convert.ToInt32(Delay.Text)
			};

			if (Repeat.IsChecked ?? false)
			{
				SetTestButtonText("Stop Test");
				_threadRunning = true;
				_thread.RunWorkerAsync(data);
				return;
			}

			await StartSingleRequest(data);
		}

		private async Task StartSingleRequest(ResolverData data)
		{
			string response = await CallResolver(data);

			if (Repeat.IsChecked ?? false)
				Browser.NavigateToString(TimedOutput(response));
			else
				Browser.NavigateToString(FullOutput(response));
		}

		private async Task<string> CallResolver(ResolverData data)
		{
			if (data.Method == "Payoff")
				return await Resolver.Payoff(data);

			return await Resolver.IsAvailable(data);
		}

		private void InitBackgroundThreadEvents()
		{
			_thread.WorkerSupportsCancellation = true;
			_thread.WorkerReportsProgress = true;

			// perform the actual work
			_thread.DoWork += (sender, args) =>
			{
				var worker = sender as BackgroundWorker;
				if (worker == null) return;
				var data = args.Argument as ResolverData;
				if (data == null) return;

				while (!worker.CancellationPending)
				{
					// call resolver synchronously
					string response = CallResolver(data).GetAwaiter().GetResult();
					worker.ReportProgress(100, response);
					Thread.Sleep(data.RepeatDelay);
				}

				args.Cancel = true;
			};

			// report progress
			_thread.ProgressChanged += (sender, args) =>
			{
				var response = args.UserState as string;
				if (Repeat.IsChecked ?? false)
					Browser.NavigateToString(TimedOutput(response));
				else
					Browser.NavigateToString(FullOutput(response));
			};

			// clean up after thread complete
			_thread.RunWorkerCompleted += (sender, args) =>
			{
				SetTestButtonText("Start Test");
				_threadRunning = false;
			};
		}

		private string FullOutput(string response)
		{
			_html.Clear();
			_html.Append(_header);
			_html.AppendFormat("<b style='color:green'>Time:</b> {0}&nbsp;&nbsp;&nbsp;", DateTime.Now.ToString("HH:mm:ss.fff"));
			_html.AppendFormat("<b style='color:green'>Elapsed:</b> {0}ms<br/>", Resolver.Elapsed.ElapsedMilliseconds);
			_html.AppendFormat("<b style='color:green'>Method:</b> {0}&nbsp;&nbsp;&nbsp;", Method.SelectedItem);
			_html.AppendFormat("<b style='color:green'>StatusCode:</b> {0}", Resolver.StatusCode);
			if (GetCell.IsChecked ?? false) _html.AppendFormat("<br/><b style='color:green'>Handling Cell:</b> {0}", Resolver.ParseCellName(response));
			if (!response.StartsWith("\r\n")) _html.Append("<br/>");
			_html.Append(response.Replace("\r\n", "<br/>").Replace("\t", "&nbsp;&nbsp;"));
			_html.Append(_footer);

			return _html.ToString();
		}

		private string TimedOutput(string response)
		{
			bool firstCall = _html.Length < 1;
			if (!firstCall) _html.Remove(0, _header.Length);

			var buff = new StringBuilder(_header);
			buff.AppendFormat("<b style='color:green'>Time:</b> {0}&nbsp;&nbsp;&nbsp;", DateTime.Now.ToString("HH:mm:ss.fff"));
			buff.AppendFormat("<b style='color:green'>Elapsed:</b> {0}ms<br/>", Resolver.Elapsed.ElapsedMilliseconds);
			buff.AppendFormat("<b style='color:green'>Method:</b> {0}&nbsp;&nbsp;&nbsp;", Method.SelectedItem);
			buff.AppendFormat("<b style='color:green'>StatusCode:</b> {0}<br/>", Resolver.StatusCode);
			if (GetCell.IsChecked ?? false) buff.AppendFormat("<b style='color:green'>Handling Cell:</b> {0}<br/>", Resolver.ParseCellName(response));
			buff.Append(!firstCall ? "<hr>" : _footer);
			_html.Insert(0, buff);

			return _html.ToString();
		}

		private void Deployment_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Deployment == null || Deployment.SelectedItem == null) return;

			if (Deployment.SelectedItem.ToString() == "dev")
			{
				Cell.Items.Clear();
				Cell.Items.Add("dev-resolver.digimarc.net");
				Cell.Items.Add("dev-resolver-usnc1.cloudapp.net");
				Cell.Items.Add("dev-resolver-eun1.cloudapp.net");
			}
			else if (Deployment.SelectedItem.ToString() == "test")
			{
				Cell.Items.Clear();
				Cell.Items.Add("test-resolver.digimarc.net");
				Cell.Items.Add("test-resolver-usnc1.cloudapp.net");
				Cell.Items.Add("test-resolver-eun1.cloudapp.net");
			}
			else if (Deployment.SelectedItem.ToString() == "labs")
			{
				Cell.Items.Clear();
				Cell.Items.Add("labs-resolver.digimarc.net");
				Cell.Items.Add("labs-resolver-usnc1.cloudapp.net");
				Cell.Items.Add("labs-resolver-eun1.cloudapp.net");
			}
			else if (Deployment.SelectedItem.ToString() == "live")
			{
				Cell.Items.Clear();
				Cell.Items.Add("resolver.digimarc.net");
				Cell.Items.Add("live-resolver-usnc1.cloudapp.net");
				Cell.Items.Add("live-resolver-ussc1.cloudapp.net");
				Cell.Items.Add("live-resolver-use1.cloudapp.net");
				Cell.Items.Add("live-resolver-usw1.cloudapp.net");
				Cell.Items.Add("live-resolver-eun1.cloudapp.net");
				Cell.Items.Add("live-resolver-euw1.cloudapp.net");
				Cell.Items.Add("live-resolver-ase1.cloudapp.net");
				Cell.Items.Add("live-resolver-asse1.cloudapp.net");
			}
		}

		private void Repeat_Click(object sender, RoutedEventArgs e)
		{
			bool isChecked = Repeat.IsChecked ?? false;
			Delay.IsEnabled = isChecked;
			DelayUnits.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
			SetTestButtonText(isChecked ? "Start Test" : "Test");
		}

		private void Method_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Method == null || Method.SelectedItem == null) return;

			bool enabled = Method.SelectedItem.ToString() == "Payoff";
			Payload.IsEnabled = enabled;
			Type.IsEnabled = enabled;
		}

		private void ClearBrowser()
		{
			_html.Clear();
			Browser.NavigateToString(_header + _footer);
		}

		private void SetTestButtonText(string text)
		{
			Test.Content = text;
			Test2.Content = text;
		}
	}
}