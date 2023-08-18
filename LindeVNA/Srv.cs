using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
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
        public static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static void SetSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings.Remove(key);
            configuration.AppSettings.Settings.Add(key, value);
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void ResponseStateFailure(ServiceGateResponse response)
        {
            bool failure = response.State == ResponseState.Failure;
            if (failure)
            {
                if (response.Error.Level == ErrorLevel.Application)
                {
                    throw new Exception(response.Error.Message);
                }
                else
                {
                    throw new Exception(response.Error.ToString());
                }
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