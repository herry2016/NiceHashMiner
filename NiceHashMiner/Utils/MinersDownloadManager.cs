﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Windows.Forms;
using NiceHashMiner.Interfaces;
using System.Threading;

namespace NiceHashMiner.Utils {
    public class MinersDownloadManager : BaseLazySingleton<MinersDownloadManager> {

        private readonly string TAG;

        private WebClient _webClient;
        private Stopwatch _stopwatch;

        const string d_01 = "https://github.com/nicehash/NiceHashMiner/releases/download/1.6.1.2/bins.zip";
        public string BinsDownloadURL = d_01;
        public string BinsZipLocation = "bins.zip";

        bool isDownloadSizeInit = false;

        IMinerUpdateIndicator _minerUpdateIndicator;

        protected MinersDownloadManager() {
            TAG = this.GetType().Name;
        }

        public void Start(IMinerUpdateIndicator minerUpdateIndicator) {
            _minerUpdateIndicator = minerUpdateIndicator;
            // #1 check bin folder
            if (!IsMinerBinFolder() && !IsMinerBinZip()) {
                Helpers.ConsolePrint(TAG, "miner bin folder NOT found");
                Helpers.ConsolePrint(TAG, "Downloading " + BinsDownloadURL);
                Downlaod();
            } else if (!IsMinerBinFolder()) {
                UnzipStart();
            }
        }

        // #1 check if miners exits 
        // TODO
        bool IsMinerBinFolder() {
            return Directory.Exists("bin");
        }

        bool IsMinerBinZip() {
            return File.Exists(BinsZipLocation);
        }

        // #2 download the file
        private void Downlaod() {
            _stopwatch = new Stopwatch();
            using (_webClient = new WebClient()) {
                _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                _webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);

                Uri downloadURL = new Uri(BinsDownloadURL);

                _stopwatch.Start();
                try {
                    _webClient.DownloadFileAsync(downloadURL, BinsZipLocation);
                    //_webClient.DownloadFile(downloadURL, BinsZipLocation);
                } catch (Exception ex) {
                    Helpers.ConsolePrint("MinersDownloadManager", ex.Message);
                }
            }
        }

        #region Download delegates

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            if (!isDownloadSizeInit) {
                isDownloadSizeInit = true;
                _minerUpdateIndicator.SetMaxProgressValue((int)(e.TotalBytesToReceive / 1024));
            }

            // Calculate download speed and output it to labelSpeed.
            var speedString = string.Format("{0} kb/s", (e.BytesReceived / 1024d / _stopwatch.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            //progressBar.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            var percString = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            var labelDownloaded = string.Format("{0} MB's / {1} MB's",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));

            _minerUpdateIndicator.SetProgressValueAndMsg(
                (int)(e.BytesReceived / 1024d),
                String.Format("{0}   {1}   {2}", speedString, percString,labelDownloaded));
            //Helpers.ConsolePrint(TAG, speedString + "   " + percString + "   " + labelDownloaded);

        }

        // The event that will trigger when the WebClient is completed
        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
            _stopwatch.Stop();
            _stopwatch = null;

            if (e.Cancelled == true) {
                // TODO handle Cancelled
                Helpers.ConsolePrint(TAG, "DownloadCompleted Cancelled");
            } else {
                // TODO handle Success
                Helpers.ConsolePrint(TAG, "DownloadCompleted Success");
                UnzipStart();
            }
        }

        #endregion Download delegates


        private void UnzipStart() {
            Thread BenchmarkThread = new Thread(UnzipThreadRoutine);
            BenchmarkThread.Start();
        }

        private void UnzipThreadRoutine() {
            if (File.Exists(BinsZipLocation)) {
                Helpers.ConsolePrint(TAG, BinsZipLocation + " already downloaded");
                Helpers.ConsolePrint(TAG, "unzipping");
                using (ZipArchive archive = ZipFile.Open(BinsZipLocation, ZipArchiveMode.Read)) {
                    //archive.ExtractToDirectory("bin");
                    _minerUpdateIndicator.SetMaxProgressValue(archive.Entries.Count);
                    int prog = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries) {
                        Helpers.ConsolePrint("ZipArchiveEntry", entry.FullName);
                        Helpers.ConsolePrint("ZipArchiveEntry", entry.Length.ToString());
                        // directory
                        if (entry.Length == 0) {
                            Directory.CreateDirectory(entry.FullName);
                        } else {
                            entry.ExtractToFile(entry.FullName);
                        }
                        _minerUpdateIndicator.SetProgressValueAndMsg(prog++, String.Format("Unzipping {0} %", ((double)(prog) / (double)(archive.Entries.Count)).ToString("F2")));
                    }
                }
            }
        }

    }
}
