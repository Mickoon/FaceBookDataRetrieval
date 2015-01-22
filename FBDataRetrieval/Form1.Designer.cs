namespace FBDataRetrieval
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.listTimeLine = new System.Windows.Forms.ListBox();
            this.btnGetList = new System.Windows.Forms.Button();
            this.UserName_or_ID = new System.Windows.Forms.TextBox();
            this.usersInfo = new System.Windows.Forms.Button();
            this.DirectMessage = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.analysePosNeg = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Location = new System.Drawing.Point(445, 9);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(533, 463);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.TabStop = false;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser1_DocumentCompleted);
            // 
            // listTimeLine
            // 
            this.listTimeLine.FormattingEnabled = true;
            this.listTimeLine.Location = new System.Drawing.Point(12, 142);
            this.listTimeLine.Name = "listTimeLine";
            this.listTimeLine.Size = new System.Drawing.Size(424, 329);
            this.listTimeLine.TabIndex = 2;
            // 
            // btnGetList
            // 
            this.btnGetList.Location = new System.Drawing.Point(15, 60);
            this.btnGetList.Name = "btnGetList";
            this.btnGetList.Size = new System.Drawing.Size(123, 71);
            this.btnGetList.TabIndex = 3;
            this.btnGetList.Text = "Get Timeline";
            this.btnGetList.UseVisualStyleBackColor = true;
            this.btnGetList.Click += new System.EventHandler(this.btnGetList_Click);
            // 
            // UserName_or_ID
            // 
            this.UserName_or_ID.Location = new System.Drawing.Point(15, 25);
            this.UserName_or_ID.Name = "UserName_or_ID";
            this.UserName_or_ID.Size = new System.Drawing.Size(164, 20);
            this.UserName_or_ID.TabIndex = 4;
            // 
            // usersInfo
            // 
            this.usersInfo.Location = new System.Drawing.Point(169, 60);
            this.usersInfo.Name = "usersInfo";
            this.usersInfo.Size = new System.Drawing.Size(120, 71);
            this.usersInfo.TabIndex = 5;
            this.usersInfo.Text = "Get User\'s information";
            this.usersInfo.UseVisualStyleBackColor = true;
            this.usersInfo.Click += new System.EventHandler(this.usersInfo_Click);
            // 
            // DirectMessage
            // 
            this.DirectMessage.Location = new System.Drawing.Point(314, 60);
            this.DirectMessage.Name = "DirectMessage";
            this.DirectMessage.Size = new System.Drawing.Size(122, 71);
            this.DirectMessage.TabIndex = 6;
            this.DirectMessage.Text = "Get Direct Messages";
            this.DirectMessage.UseVisualStyleBackColor = true;
            this.DirectMessage.Click += new System.EventHandler(this.DirectMessage_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(209, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Please write screenname or user integer ID";
            // 
            // analysePosNeg
            // 
            this.analysePosNeg.Location = new System.Drawing.Point(242, 9);
            this.analysePosNeg.Name = "analysePosNeg";
            this.analysePosNeg.Size = new System.Drawing.Size(194, 36);
            this.analysePosNeg.TabIndex = 8;
            this.analysePosNeg.Text = "Analyse Pos/Neg Words";
            this.analysePosNeg.UseVisualStyleBackColor = true;
            this.analysePosNeg.Click += new System.EventHandler(this.analysePosNeg_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(990, 484);
            this.Controls.Add(this.analysePosNeg);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DirectMessage);
            this.Controls.Add(this.usersInfo);
            this.Controls.Add(this.UserName_or_ID);
            this.Controls.Add(this.btnGetList);
            this.Controls.Add(this.listTimeLine);
            this.Controls.Add(this.webBrowser1);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.ListBox listTimeLine;
        private System.Windows.Forms.Button btnGetList;
        private System.Windows.Forms.TextBox UserName_or_ID;
        private System.Windows.Forms.Button usersInfo;
        private System.Windows.Forms.Button DirectMessage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button analysePosNeg;
    }
}

