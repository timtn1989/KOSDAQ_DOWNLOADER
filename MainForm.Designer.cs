
namespace 코스닥다운로더
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.progressBar_part = new System.Windows.Forms.ProgressBar();
            this.label_date = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.progressBar_all = new System.Windows.Forms.ProgressBar();
            this.API = new AxKHOpenAPILib.AxKHOpenAPI();
            this.label_time = new System.Windows.Forms.Label();
            this.label_target = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.API)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar_part
            // 
            this.progressBar_part.Location = new System.Drawing.Point(6, 50);
            this.progressBar_part.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressBar_part.Maximum = 800;
            this.progressBar_part.Name = "progressBar_part";
            this.progressBar_part.Size = new System.Drawing.Size(418, 21);
            this.progressBar_part.TabIndex = 0;
            // 
            // label_date
            // 
            this.label_date.AutoSize = true;
            this.label_date.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_date.Location = new System.Drawing.Point(140, 85);
            this.label_date.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_date.Name = "label_date";
            this.label_date.Size = new System.Drawing.Size(80, 16);
            this.label_date.TabIndex = 1;
            this.label_date.Text = "00000000";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(6, 73);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "부분";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new System.Drawing.Point(6, 33);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "전체";
            // 
            // progressBar_all
            // 
            this.progressBar_all.Location = new System.Drawing.Point(6, 10);
            this.progressBar_all.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressBar_all.Name = "progressBar_all";
            this.progressBar_all.Size = new System.Drawing.Size(418, 21);
            this.progressBar_all.TabIndex = 3;
            // 
            // API
            // 
            this.API.Enabled = true;
            this.API.Location = new System.Drawing.Point(721, 181);
            this.API.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.API.Name = "API";
            this.API.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("API.OcxState")));
            this.API.Size = new System.Drawing.Size(79, 31);
            this.API.TabIndex = 5;
            // 
            // label_time
            // 
            this.label_time.AutoSize = true;
            this.label_time.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_time.Location = new System.Drawing.Point(344, 85);
            this.label_time.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_time.Name = "label_time";
            this.label_time.Size = new System.Drawing.Size(74, 16);
            this.label_time.TabIndex = 6;
            this.label_time.Text = "00:00:00";
            // 
            // label_target
            // 
            this.label_target.AutoSize = true;
            this.label_target.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_target.Location = new System.Drawing.Point(224, 85);
            this.label_target.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_target.Name = "label_target";
            this.label_target.Size = new System.Drawing.Size(71, 16);
            this.label_target.TabIndex = 7;
            this.label_target.Text = "준비중..";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(431, 105);
            this.Controls.Add(this.label_target);
            this.Controls.Add(this.label_time);
            this.Controls.Add(this.API);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBar_all);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label_date);
            this.Controls.Add(this.progressBar_part);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "MainForm";
            this.Text = "코스닥다운로더";
            ((System.ComponentModel.ISupportInitialize)(this.API)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar_part;
        private System.Windows.Forms.Label label_date;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar progressBar_all;
        private AxKHOpenAPILib.AxKHOpenAPI API;
        private System.Windows.Forms.Label label_time;
        private System.Windows.Forms.Label label_target;
    }
}

