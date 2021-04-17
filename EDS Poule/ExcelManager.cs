﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.IO;
using System.Configuration;

namespace EDS_Poule
{
    public class ExcelReadSettings
    {
        public readonly int StartRow = Convert.ToInt32(ConfigurationManager.AppSettings.Get("StartRow"));
        public readonly int BlockSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get("BlockSize"));
        public readonly int NrBlocks = Convert.ToInt32(ConfigurationManager.AppSettings.Get("NrBlocks"));
        public readonly int FirstHalfSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get("FirstHalfSize"));
        public readonly int HomeColumn = Convert.ToInt32(ConfigurationManager.AppSettings.Get("HomeClumn"));
        public readonly int OutColumn = Convert.ToInt32(ConfigurationManager.AppSettings.Get("OutColumn"));
        public int Miss = 0;
        public readonly int HalfWayJump = Convert.ToInt32(ConfigurationManager.AppSettings.Get("HalfWayJump"));

        public readonly string AdminFileName = ConfigurationManager.AppSettings.Get("AdminLocation");
        public readonly int Hostsheet = Convert.ToInt32(ConfigurationManager.AppSettings.Get("HostSheet"));
        public readonly int Rankingheet = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Rankingsheet"));
        public readonly int Topscorerssheet = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Topscorerssheet"));
    }

    public class ExcelManager
    {
        private ExcelReadSettings Settings;
        private excel.Application xlApp;
        private excel.Workbook xlWorkbook;
        private excel._Worksheet xlWorksheet;
        private excel.Range xlRange;

        public ExcelManager(int miss = 0)
        {
            Settings = new ExcelReadSettings();
            Settings.Miss = miss;
        }

        public IEnumerable<int> ExportPlayersToExcel(List<Player> Players)
        {
            InitialiseWorkbook(Settings.AdminFileName, Settings.Rankingheet);
            int y = 2;
            foreach (Player player in Players)
            {
                xlRange.Cells[y, 1].value2 = player.Ranking;
                xlRange.Cells[y, 2].value2 = player.PreviousRanking;
                xlRange.Cells[y, 3].value2 = player.Name;
                xlRange.Cells[y, 4].value2 = player.Woonplaats;
                xlRange.Cells[y, 5].value2 = player.TotalScore;
                xlRange.Cells[y, 6].value2 = player.WeekScore;
                y++;
                yield return y;
            }
            CleanWorkbook();
        }

        public Week[] ReadPredictions(string filename, int sheet, bool firsthalf = false, bool secondhalf = false, Week[] Weeks = null)
        {
            var weeks = new Week[Settings.NrBlocks];
            if (Weeks != null)
                weeks = Weeks;

            var StartWeek = 0;
            var Endweek = Settings.NrBlocks;
            if (firsthalf)
                Endweek -= (Settings.NrBlocks - Settings.FirstHalfSize);
            if (secondhalf)
                StartWeek += Settings.FirstHalfSize;
            

            try
            {
                InitialiseWorkbook(filename, sheet);
                for (int i = StartWeek; i < Endweek; i++)
                {
                    var matches = ReadSingleWeek(filename, sheet, i, false);
                    weeks[i] = new Week((i + 1), matches);
                }
                CleanWorkbook();
                return weeks;
            }

            catch { CleanWorkbook(); return weeks; }
        }

        public Match[] ReadSingleWeek(string filename, int sheet, int week, bool initialize = true)
        {
            Match[] Week = new Match[9];

            if(initialize)
                InitialiseWorkbook(filename, sheet);

            int startrow = Settings.StartRow + (Settings.BlockSize + 1) * (week) + Settings.Miss;
            if (week >= Settings.FirstHalfSize)
                startrow += Settings.HalfWayJump;
            try
            {
                for (int rowschecked = 0; rowschecked < Settings.BlockSize; rowschecked++)
                {
                    double x = 99;
                    double y = 99;
                    int currentRow = startrow + rowschecked;

                    if (xlRange.Cells[currentRow, Settings.HomeColumn].Value2 != null && xlRange.Cells[currentRow, Settings.OutColumn].Value2 != null)
                    {
                        try
                        {
                            x = xlRange.Cells[currentRow, Settings.HomeColumn].Value2;
                            y = xlRange.Cells[currentRow, Settings.OutColumn].Value2;
                        }
                        catch { };
                    }

                    bool motw = false;
                    if (rowschecked == Settings.BlockSize - 1)
                    {
                        motw = true;
                    }

                    Match match = new Match(Convert.ToInt32(x), Convert.ToInt32(y), motw);
                    Week[rowschecked] = match;
                }
                if(initialize)
                    CleanWorkbook();
                return Week;
            }
            catch { CleanWorkbook(); return Week; }
        }

        public BonusQuestions ReadBonus()
        {
            int column = 7;
            int weekcolumn = 10;
            InitialiseWorkbook(Settings.AdminFileName, Settings.Hostsheet);
            try
            {
                int[] weeks =
                {
                    Convert.ToInt32(xlRange.Cells[366, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[367, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[368, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[369, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[370, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[371, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[372, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[373, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[375, weekcolumn].value2),
                    Convert.ToInt32(xlRange.Cells[377, weekcolumn].value2),
            };

                string[] degradanten = { Convert.ToString(xlRange.Cells[375, column].value2), Convert.ToString(xlRange.Cells[376, column].value2) };
                string[] promovendi = { Convert.ToString(xlRange.Cells[377, column].value2), Convert.ToString(xlRange.Cells[378, column].value2) };
                string[] finalists = { Convert.ToString(xlRange.Cells[373, column].value2), Convert.ToString(xlRange.Cells[374, column].value2) };
                BonusQuestions bonus = new BonusQuestions
                    (
                    Convert.ToString(xlRange.Cells[366, column].value2),
                    Convert.ToString(xlRange.Cells[367, column].value2),
                    Convert.ToString(xlRange.Cells[368, column].value2),
                    Convert.ToString(xlRange.Cells[369, column].value2),
                    Convert.ToString(xlRange.Cells[370, column].value2),
                    Convert.ToString(xlRange.Cells[371, column].value2),
                    Convert.ToString(xlRange.Cells[372, column].value2),
                    finalists, degradanten, promovendi, 
                    weeks
                    );
                CleanWorkbook();
                return bonus;
            }
            catch { CleanWorkbook(); return null; };
        }

        public Dictionary<string, Topscorer> readtopscorers(int nrScorers)
        {
            Dictionary<string, Topscorer> scorers = new Dictionary<string, Topscorer>();
            InitialiseWorkbook(Settings.AdminFileName, Settings.Topscorerssheet);
            for (int i = 2; i < (nrScorers+2); i++)
            {
                Topscorer ts = new Topscorer() { Total = 0, Rounds = new List<int>()};
                string name = Convert.ToString(xlRange.Cells[i, 1].value2);
                ts.Total = Convert.ToInt32(xlRange.Cells[i, 3].value2);
                for (int x = 0; x < 34; x++)
                {
                    var round = Convert.ToInt32(xlRange.Cells[i, x + 4].value2);
                    ts.Rounds.Add(round);               
                }
                scorers.Add(name, ts);
            }

            CleanWorkbook();
            return scorers;
        }

        private void InitialiseWorkbook(string filename, int sheet)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException();

            xlApp = new excel.Application();
            xlWorkbook = xlApp.Workbooks.Open(filename);
            xlWorksheet = xlWorkbook.Sheets[sheet];
            xlRange = xlWorksheet.UsedRange;
        }

        private void CleanWorkbook()
        {
            //cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //release com objects to fully kill excel process from running in the background
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            //close and release
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            //quit and release
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
        }
    }
}
