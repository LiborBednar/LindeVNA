﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Deployment.Application;
using System.Reflection;

using IDisposable = System.IDisposable;
using FrameworkElement = System.Windows.FrameworkElement;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Debug = System.Diagnostics.Debug;

using Noris.WS.ServiceGate;
using Noris.Clients.ServiceGate;


namespace LindeVNA
{
    public class WaitCursorIndicator : IDisposable
    {
        FrameworkElement Onwer;
        Cursor Previous;

        public WaitCursorIndicator(FrameworkElement owner)
        {
            this.Onwer = owner;
            if (owner == null) return;
            Previous = owner.Cursor;
            owner.Cursor = Cursors.Wait;
        }

        void IDisposable.Dispose()
        {
            if (this.Onwer == null) return;
            this.Onwer.Cursor = Previous;
        }
    }

    public static class Srv : Object
    {
        public static void UpdateSettings()
        {
            if (Properties.Settings.Default.UpgradeSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                Properties.Settings.Default.Save();
            }
        }

        //public static void SetSettings(string key, object value)
        //{
        //    Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        //    MessageBox.Show(configuration.FilePath);
        //    configuration.AppSettings.Settings.Remove(key);
        //    configuration.AppSettings.Settings.Add(key, value.ToString());
        //    configuration.Save(ConfigurationSaveMode.Full, true);
        //    ConfigurationManager.RefreshSection("appSettings");
        //}

        //public static string GetSettings(string key)
        //{
        //    string ret = null;
        //    if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>(key))
        //    {
        //        ret = ConfigurationManager.AppSettings[key];
        //    }
        //    return ret;
        //}

        //public static Int32 GetSettings(string key, Int32 defaultValue)
        //{
        //    Int32 ret = defaultValue;
        //    if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>(key))
        //    {
        //        try
        //        {
        //            ret = Convert.ToInt32(ConfigurationManager.AppSettings[key]);
        //        }
        //        catch (InvalidCastException) { }
        //    }
        //    return ret;
        //}

        //public static bool GetSettings(string key, bool defaultValue)
        //{
        //    bool ret = defaultValue;
        //    if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>(key))
        //    {
        //        try
        //        {
        //            ret = Convert.ToBoolean(ConfigurationManager.AppSettings[key]);
        //        }
        //        catch (InvalidCastException) { }
        //    }
        //    return ret;
        //}


        public static string LocalApplicationDataPath()
        {
            System.Reflection.AssemblyCompanyAttribute companyAttribute = (System.Reflection.AssemblyCompanyAttribute)System.Reflection.AssemblyCompanyAttribute.GetCustomAttribute(System.Reflection.Assembly.GetExecutingAssembly(), typeof(System.Reflection.AssemblyCompanyAttribute));
            string sCompanyName = companyAttribute.Company;
            string sProductName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.ToString();

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), sCompanyName, sProductName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static Version GetRunningVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch (Exception)
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
    }

    public class InputTable : DataTable
    {
        public InputTable() { }

        public InputTable(string tableName) : base(tableName) { }

        public int AddRow()
        {
            DataRow r = base.NewRow();
            this.Rows.Add(r);
            return this.Rows.IndexOf(r);
        }

        public DataColumn AddColumn(string colName, Type datatype)
        {
            DataColumn col = new DataColumn(colName, datatype);
            this.Columns.Add(col);
            return col;
        }

        public static void SetItem(DataRow row, string columnName, object value)
        {
            if (row != null)
            {
                row[columnName] = value;
            }
        }

        public void SetItem(int row, string columnName, object value)
        {
            SetItem(this.Rows[row], columnName, value);
        }

        public static DateTime GetItemDateTime(DataRow row, string columnName, DateTime defaultValue)
        {
            if (row.IsNull(columnName))
                return defaultValue;
            else
                return (DateTime)row[columnName];
        }

        public static string GetItemString(DataRow row, string columnName, string defaultValue)
        {
            if (row.IsNull(columnName))
                return defaultValue;
            else
                return row[columnName].ToString();
        }

        public static int GetItemInt32(DataRow row, string columnName, int defaultValue)
        {
            if (row.IsNull(columnName))
                return defaultValue;
            else
                return (int)row[columnName];
        }

        public static decimal GetItemDecimal(DataRow row, string columnName, decimal defaultValue)
        {
            if (row.IsNull(columnName))
                return defaultValue;
            else
                return (decimal) row[columnName];
        }

        public string GetItemString(int row, string columnName, string defaultValue)
        {
            return GetItemString(this.Rows[row], columnName, defaultValue);
        }

        public int GetItemInt32(int row, string columnName, int defaultValue)
        {
            return GetItemInt32(this.Rows[row], columnName, defaultValue);
        }

        public decimal GetItemDecimal(int row, string columnName, decimal defaultValue)
        {
            return GetItemDecimal(this.Rows[row], columnName, defaultValue);
        }
    }

}