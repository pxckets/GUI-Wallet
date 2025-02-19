﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SpookyCoin_Gui_Wallet
{
    public partial class Wallet : Form
    {
        // Set placeholder strings
        public string walletAddressStr;
        public string paymentIdStr;
        public string amountStr;
        public string feeStr;

        public Wallet()
        {
            InitializeComponent();
            
            // Get Primary Address
            string primaryAddress = ApiClient.HTTP("", "/addresses/primary", "GET");
            if (primaryAddress.StartsWith("{"))
            {
                JObject JsonParse = JObject.Parse(primaryAddress);
                string address = (string)JsonParse["address"];

                primaryAddressValue.Text = address;
                Config.PrimaryAddress = address;
            }

            // Add column to transactions
            transactionsGrid.ColumnCount = 4;
            transactionsGrid.Columns[0].Name = "Date";
            transactionsGrid.Columns[1].Name = "Type";
            transactionsGrid.Columns[2].Name = "Address";
            transactionsGrid.Columns[3].Name = "Amount";
            transactionsGrid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Get height, balance, transactions
            GetInformation();

            // Set Mixin to 1
            mixinLst.SelectedIndex = 1;

            // Set placeholders & colors
            walletAddressStr = "Enter a wallet address (Example: Sp3zsmMPTeN1EnKbFENrjMNaFfEvBd3iPEJnzqaqmKk2BHaKsNdFozFZzBRPBZvMKH2DQ3rZg5onJMwSfBMYLLv6114LE6i45)";
            walletAddressTxt.Text = walletAddressStr;
            walletAddressTxt.ForeColor = Color.Gray;
            paymentIdStr = "Enter a payment ID";
            paymentIdTxt.Text = paymentIdStr;
            paymentIdTxt.ForeColor = Color.Gray;
            amountStr = "1234.00";
            amountTxt.Text = amountStr;
            amountTxt.ForeColor = Color.Gray;
            feeStr = "Min. 1 SPKY";
            feeTxt.Text = feeStr;
            feeTxt.ForeColor = Color.Gray;
        }

        public string[] addRow;
        public void addTransaction(string date, string type, string address, string amount)
        {
            addRow = new string[] {date, type, address, amount};
            transactionsGrid.Rows.Add(addRow);
        }
        public void emptyTransactions()
        {
            transactionsGrid.Rows.Clear();
        }
        public int currentSelectedRow()
        {
            return transactionsGrid.CurrentCell.RowIndex;
        }
        public void selectCurrentRow(int row)
        {
            transactionsGrid.Rows[row].Selected = true;
        }
        
        public async Task GetInformation()
        {
            while (true)
            {
                // Get Coin Information
                string coinInformation = ApiClient.HTTP("", "/status", "GET");
                if (coinInformation.StartsWith("{"))
                {
                    JObject JsonParse = JObject.Parse(coinInformation);
                    int networkBlockCount = (int)JsonParse["networkBlockCount"];
                    int hashrate = (int)JsonParse["hashrate"];

                    blockchainHeightValue.Text = String.Format("{0:n0}", networkBlockCount);
                    hashrateValue.Text = String.Format("{0:n0}", hashrate);
                }

                // Get Balance Information
                string balanceInformation = ApiClient.HTTP("", "/balance", "GET");
                if (balanceInformation.StartsWith("{"))
                {
                    JObject JsonParse = JObject.Parse(balanceInformation);
                    int unlocked = (int)JsonParse["unlocked"];
                    int locked = (int)JsonParse["locked"];

                    unlockedValue.Text = String.Format("{0,0:N2}", unlocked / 100.0) + " SPKY";
                    lockedValue.Text = String.Format("{0,0:N2}", locked / 100.0) + " SPKY";
                }

                // Get Transactions
                string transactionsInformation = ApiClient.HTTP("", "/transactions", "GET");
                if (transactionsInformation.StartsWith("{"))
                {
                    emptyTransactions();
                    
                    string jsonTransactions = "["+transactionsInformation.Replace("\n", "")+"]";
                    JArray objectTransactions = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(jsonTransactions);
                    foreach (var resultTransactions in objectTransactions)
                    {
                        foreach (JObject transactions in resultTransactions["transactions"])
                        {
                            string blockHeight = (string)transactions["blockHeight"];
                            int fee = (int)transactions["fee"];
                            string hash = (string)transactions["hash"];
                            int timestamp = (int)transactions["timestamp"];
                            string amount = null;

                            foreach (JObject kanker in transactions["transfers"])
                            {
                                amount = String.Format("{0,0:N2}", (int)kanker["amount"] / 100.0);
                            }

                            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds(timestamp).ToLocalTime();
                            string timestampToDateTime = dtDateTime.Day + "-" + dtDateTime.Month + "-" + dtDateTime.Year + " " + dtDateTime.Hour + ":" + dtDateTime.Minute;

                            addTransaction(timestampToDateTime, "Receive", "-", amount + " SPKY");
                        }
                    }

                    //selectCurrentRow(currentSelectedRow());
                }
                
                await Task.Delay(3000);
            }
        }

        public static System.Windows.Forms.Timer changeToBlack = new System.Windows.Forms.Timer();
        
        private void CopyAddress(object sender, MouseEventArgs e)
        {
            primaryAddressValue.ForeColor = Color.Green;
            changeToBlack.Tick += new EventHandler(changeToBlack_Tick);
            changeToBlack.Interval = 500;
            changeToBlack.Start();
            Clipboard.SetText(Config.PrimaryAddress);
        }

        private void Closed(object sender, FormClosingEventArgs e)
        {
            ApiClient.HTTP("", "/save", "PUT"); // Logout of wallet in API
            //ApiClient.HTTP("", "/wallet", "DELETE"); // Logout of wallet in API
            /*foreach (var process in Process.GetProcessesByName("wallet-api"))
            {
                process.Kill();
            }*/
            Environment.Exit(1);
        }

        private void changeToBlack_Tick(object sender, EventArgs e)
        {
            changeToBlack.Stop();
            primaryAddressValue.ForeColor = Color.Black;
        }

        private void aboutMenu_Click(object sender, EventArgs e)
        {
            About aboutWindow = new About();
            aboutWindow.Show();
        }

        private void placeholderWalletAddressEnter(object sender, EventArgs e)
        {
            if (walletAddressTxt.Text == walletAddressStr)
            {
                walletAddressTxt.Text = "";
                walletAddressTxt.ForeColor = Color.Black;
            }
        }

        private void placeholderWalletAddressLeave(object sender, EventArgs e)
        {
            if (walletAddressTxt.Text == "")
            {
                walletAddressTxt.Text = walletAddressStr;
                walletAddressTxt.ForeColor = Color.Gray;
            }
        }

        private void paymentIdEnter(object sender, EventArgs e)
        {
            if (paymentIdTxt.Text == paymentIdStr)
            {
                paymentIdTxt.Text = "";
                paymentIdTxt.ForeColor = Color.Black;
            }
        }

        private void paymentIdLeave(object sender, EventArgs e)
        {
            if (paymentIdTxt.Text == "")
            {
                paymentIdTxt.Text = paymentIdStr;
                paymentIdTxt.ForeColor = Color.Gray;
            }
        }

        private void amountEnter(object sender, EventArgs e)
        {
            if (amountTxt.Text == amountStr)
            {
                amountTxt.Text = "";
                amountTxt.ForeColor = Color.Black;
            }
        }

        private void amountLeave(object sender, EventArgs e)
        {
            if (amountTxt.Text == "")
            {
                amountTxt.Text = amountStr;
                amountTxt.ForeColor = Color.Gray;
            }
        }

        private void feeEnter(object sender, EventArgs e)
        {
            if (feeTxt.Text == feeStr)
            {
                feeTxt.Text = "";
                feeTxt.ForeColor = Color.Black;
            }
        }

        private void feeLeave(object sender, EventArgs e)
        {
            if (feeTxt.Text == "")
            {
                feeTxt.Text = feeStr;
                feeTxt.ForeColor = Color.Gray;
            }
        }
    }
}
