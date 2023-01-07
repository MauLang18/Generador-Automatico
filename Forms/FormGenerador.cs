using FireSharp.Config;
using FireSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FireSharp.Response;
using FireSharp.Interfaces;
using FireSharp;
using FireSharp.Config;
using Newtonsoft.Json;
using System.Collections;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Net;
using System.IO;

namespace GeneradorDeBCF_CF.Forms
{
    public partial class FormGenerador : Form
    {
        string newBcf = "0001";
        string iid;
        IFirebaseConfig config = new FirebaseConfig
        {
            //codigo secreto de firebase
            AuthSecret = "",
            //link de sub proyecto de firebase
            BasePath = ""
        };

        IFirebaseClient client;
        public FormGenerador()
        {
            InitializeComponent();
        }

        private void FormGeneradorBCF_Load(object sender, EventArgs e)
        {
            client = new FireSharp.FirebaseClient(config);

            if (client != null)
            {
                grid();
                Console.WriteLine(newBcf);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var datalayer = new Data
            {
                Id = newBcf,
                Cliente = txtCliente.Text,
                Fecha = txtFecha.Text,
                IDTRA = txtIDTRA.Text
            };

            string resp22 = await datosDy(txtIDTRA.Text);

            Root id = JsonConvert.DeserializeObject<Root>(resp22);

            foreach (var item in id.value)
            {
                iid = item.incidentid;
            }

            Console.WriteLine(iid);

            string resp2 = await CreateAsync(iid, newBcf);

            Console.WriteLine(resp2);

            //SetResponse resp = await client.SetAsync("Information/"+newBcf, datalayer);
            //Data result = resp.ResultAs<Data>();
            clean();

            //grid();
        }

        public void clean()
        {
            txtIDTRA.Text = "";
            txtCliente.Text = "";
        }
        
        public void grid()
        {
            FirebaseResponse response = client.Get(@"Information");
            Dictionary<string, Data> data = JsonConvert.DeserializeObject<Dictionary<string, Data>>(response.Body.ToString());
            PopulateDataGrid(data);
        }

        void PopulateDataGrid(Dictionary<string, Data> record)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            dataGridView1.Columns.Add("Id", "HBL");
            dataGridView1.Columns.Add("Cliente", "CLIENTE");
            dataGridView1.Columns.Add("Fecha", "FECHA");
            dataGridView1.Columns.Add("ID", "ID");

            if (record == null)
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                dataGridView1.Columns.Add("Id", "HBL");
                dataGridView1.Columns.Add("Cliente", "CLIENTE");
                dataGridView1.Columns.Add("Fecha", "FECHA");
                dataGridView1.Columns.Add("ID", "ID");
            }
            else
            {
                foreach (var item in record)
                {
                    dataGridView1.Rows.Add(item.Key, item.Value.Cliente, item.Value.Fecha, item.Value.IDTRA);

                    if (item.Value.IDTRA != txtIDTRA.Text)
                    {
                        generar(item.Key);
                    }
                    else
                    {
                        MessageBox.Show("Ese IDTRA ya posee BL Hijo");
                    }
                }
            }

        }

        private void generar(string lastBcf)
        {
            var bcf1 = Convert.ToInt32(lastBcf);
            var bcf2 = bcf1 += 1;
            //var bcf2 = 100;
            var bcf3 = "0000";
            if (bcf2 <= 9)
            {
                bcf3 = "000" + bcf2.ToString();
            }
            else if (bcf2 <= 99)
            {
                bcf3 = "00" + bcf2.ToString();
            }
            else if (bcf2 <= 999)
            {
                bcf3 = "0" + bcf2.ToString();
            }
            else if (bcf2 <= 9999)
            {
                bcf3 = bcf2.ToString();
            }
            Console.WriteLine(bcf3);
            newBcf = bcf3.ToString();
        }

        private async Task<string> datosDy(string id)
        {
            //link del api
            WebRequest oRequest = WebRequest.Create(new Uri("" + id + ""));
            WebResponse oResponse = oRequest.GetResponse();
            StreamReader sr = new StreamReader(oResponse.GetResponseStream());
            return await sr.ReadToEndAsync();
        }

        //Conexion a Api Rest de Dynamics 365
        public async Task<string> CreateAsync(string id, string bcf)
        {
            //id de la api web
            string clientId = "";
            //clave secreta de la api web
            string clientSecrets = "";
            //login
            string authority = "";
            //direccion url
            string crmUrl = "";
            string response2 = "";

            string accessToken = string.Empty;

            ClientCredential credentials = new ClientCredential(clientId, clientSecrets);
            var authContext = new AuthenticationContext(authority);
            var result = await authContext.AcquireTokenAsync(crmUrl, credentials);
            accessToken = result.AccessToken;
            Console.WriteLine(accessToken);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(crmUrl);
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string entityName = "goals";

                dynamic content = new JObject();
                content.title = "GCF2023." + bcf;
                content["new_Idtra@odata.bind"] = "/incidents(" + id + ")";
                content["metricid@odata.bind"] = "/metrics(5d3efa23-0887-ed11-81ac-002248046c8d)";
                //content.new_asignar = true;
                content.fiscalperiod = 1;
                HttpContent httpContent = new StringContent(content.ToString(), UnicodeEncoding.UTF8, "application/json");
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync($"api/data/v9.2/{entityName}", httpContent);
                var response = httpResponseMessage.EnsureSuccessStatusCode();

                response2 = response.ToString();

                Console.WriteLine(response);
            }

            return response2;
        }
    }
}
