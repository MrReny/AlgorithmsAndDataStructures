using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using Shenon_Fano_Coding.Model;
using Shenon_Fano_Coding.Services;
using Splat;

namespace Shenon_Fano_Coding.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Dictionary<char, long> _commodityDictionary;

        private Dictionary<char, long> CommodityDictionary
        {
            get => _commodityDictionary;
            set
            {
                if (value == _commodityDictionary) return;
                _commodityDictionary = value;
                OnPropertyChanged();

                OnPropertyChanged("Labels");
            }
        }

        private SeriesCollection _commoditySeries;
        private string _fileName;
        private string _effectivnes;


        public SeriesCollection CommoditySeries
        {
            get => _commoditySeries;
            set
            {
                if (_commoditySeries == value) return;
                _commoditySeries = value;
                OnPropertyChanged();
            }
        }


        public string[] Labels => _commodityDictionary.Keys.Select(c => "" + c).ToArray();


        public string FileName
        {
            get => _fileName;
            set
            {
                if(_fileName==value) return;
                _fileName = value;
                OnPropertyChanged();
            }
        }

        public string Effectivnes
        {
            get => _effectivnes;
            set
            {
                if(_effectivnes ==value) return;
                _effectivnes = value;
                OnPropertyChanged();
            }
        }


        public DelegateCommand OpenFile => new DelegateCommand(() =>
        {
            try
            {
                var fileDialog = new OpenFileDialog();

                if (fileDialog.ShowDialog() != null)
                {
                    FileName = fileDialog.FileName;
                }
            }
            catch (Exception e)
            {
                Locator.Current.GetService<INotificationService>()?.ShowError(e.Message);
            }
        });

        public DelegateCommand EncodeFile => new DelegateCommand(async () => { Encode(); });

        public DelegateCommand DecodeFile => new DelegateCommand(async () =>
        {
            try
            {
                var e = new Encoder(FileName);

                await e.Decode();
                Locator.Current.GetService<INotificationService>()?.ShowSuccess("Декодирование завершено");
            }
            catch (Exception exception)
            {
                Locator.Current.GetService<INotificationService>()?.ShowError(exception.Message);
            }
        });

        public MainWindowViewModel()
        {
            FillCommoditySeries();
        }

        private void FillCommoditySeries()
        {
            try
            {
                CommoditySeries = new SeriesCollection()
                {
                    new LineSeries()
                    {
                        Values = new ChartValues<long>(_commodityDictionary.Values),
                        DataLabels = true,
                    }
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async void Encode()
        {
            try
            {
                var e = new Encoder(FileName);
                var rs = new StreamReader(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite,
                    FileShare.ReadWrite));

                rs.Peek();

                e.FrequenciesCounted += (o, ea) =>
                {
                    Dispatcher.CurrentDispatcher
                        .Invoke(() =>
                            {
                                CommodityDictionary = new Dictionary<char, long>(e.FrequencyList);
                                FillCommoditySeries();

                                Locator.Current.GetService<INotificationService>()
                                    ?.ShowSuccess("Частоты сформированны");
                            }
                        );
                };

                int cnt;
                try
                {
                    cnt = rs.CurrentEncoding.GetByteCount(new char[] {(char) rs.Peek()}) *8;
                }
                catch (Exception exception)
                {
                    cnt = 4 * 8;
                }
                await e.Encode(cnt);

                rs.Close();

                var fl = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite,
                    FileShare.ReadWrite).Length;
                var sl = new FileStream(FileName + ".fano", FileMode.Open, FileAccess.ReadWrite,
                    FileShare.ReadWrite).Length;
                Effectivnes =  $"Эффективность {(float)fl/(fl-sl) *10}%" ;
            }
            catch (Exception exception)
            {
                Dispatcher.CurrentDispatcher
                    .InvokeAsync(() =>
                        Locator.Current.GetService<INotificationService>()?.ShowError(exception.Message));
            }
        }
    }
}