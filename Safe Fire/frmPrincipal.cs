using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.VisualBasic.FileIO;
using System.Data.OleDb;
using System.Globalization;
using MySql.Data.MySqlClient;


namespace Aplicación {
    public partial class frmPrincipal : Form {

        enum tipoSQL {
            nonquery, reader
        }

        private GMapOverlay markerOverlay;

        public frmPrincipal() {
            InitializeComponent();
        }



        private DataTable ejecutarMySQL(tipoSQL tipo, string sql) {

            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = "sql100.fricehost.cu.cc";
            builder.UserID = "frice_20036265";
            builder.Password = "riverplate";
            builder.Database = "frice_20036265_fire";

            //MySqlConnection conexion = new MySqlConnection("data source=sql100.fricehost.cu.cc;user id=frice_20036265; password='riverplate'; database=frice_20036265_fire");

            MySqlConnection conexion = new MySqlConnection("Server=sql100.fricehost.cu.cc;Database=frice_20036265_fire; Uid=frice_20036265;Pwd='riverplate';");
            //MySqlConnection conexion = new MySqlConnection(builder.ToString());

            MySqlDataAdapter da;
            MySqlCommand comando = new MySqlCommand();
            DataTable tabla;

            conexion.Open();
            //da = new MySqlDataAdapter(sql, conexion);
            tabla = new DataTable();
            //da.Fill(tabla);

            comando.Connection = conexion;
            comando.CommandType = CommandType.Text;
            comando.CommandText = sql;

            if (tipo == tipoSQL.reader) {
                tabla = new DataTable();
                tabla.Load(comando.ExecuteReader());

            }
            else {
                tabla = new DataTable();
                comando.ExecuteNonQuery();
            }

            conexion.Close();
            return tabla;


        }


        private DataTable ejecutarSQL(tipoSQL tipo, string sql) {
            string cadena = "Provider=SQLNCLI11;Data Source=franco-pc;Integrated Security=SSPI;Initial Catalog=HEPHAESTUS";
            OleDbConnection conexion = new OleDbConnection(cadena);
            OleDbCommand comando = new OleDbCommand();

            conexion.Open();
            comando.Connection = conexion;
            comando.CommandType = CommandType.Text;
            comando.CommandText = sql;

            DataTable tabla;

            if (tipo == tipoSQL.reader) {
                tabla = new DataTable();
                tabla.Load(comando.ExecuteReader());
                
            }
            else {
                tabla = new DataTable();
                comando.ExecuteNonQuery();
            }

            conexion.Close();
            return tabla;
            
            
        }

        private void frmPrincipal_Load(object sender, EventArgs e) {
            map.DragButton = MouseButtons.Left;
            map.CanDragMap = true;
            map.MapProvider = GMapProviders.GoogleMap;
            map.Position = new PointLatLng(-31.3843155, -64.1774003);
            map.MinZoom = 4;
            map.MaxZoom = 20;
            map.Zoom = 4;
            map.AutoScroll = true;

            markerOverlay = new GMapOverlay("Marcador");
            map.Overlays.Add(markerOverlay);



        }

        private void grabarPuntos(string filePath) {
            ejecutarSQL(tipoSQL.nonquery, "DELETE FROM PUNTOS");
            int i = 0;
            bool cabecera = false;
            String fieldSeparator = ",";
            using (TextFieldParser csvReader = new TextFieldParser(filePath)) {
                csvReader.HasFieldsEnclosedInQuotes = true;
                while (!csvReader.EndOfData) {
                    csvReader.SetDelimiters(new string[] { fieldSeparator });
                    string[] fieldData = csvReader.ReadFields();

                    if (cabecera) { 
                        i += 1;
                        string sql = "";
                        sql += "INSERT INTO PUNTOS VALUES(" + i + ", " + fieldData[0] + ", " + fieldData[1] + ")";
                        ejecutarSQL(tipoSQL.nonquery, sql);
                    }

                    if (!cabecera) cabecera = true;

                }
            }

        }


        private void cmdCargar_Click(object sender, EventArgs e) {
            cmdCargar.Enabled = false;
            cmdCargar.Text = "Cargando...";

            DataTable tabla = ejecutarSQL(tipoSQL.reader, "SELECT LATITUD, LONGITUD FROM PUNTOS");

            GMarkerGoogle mark;
            for (int i = 0; i < tabla.Rows.Count; i++) {

                float latitud = Convert.ToSingle(tabla.Rows[i][0].ToString());
                float longitud = Convert.ToSingle(tabla.Rows[i][1].ToString());

                PointLatLng pos = new PointLatLng(latitud, longitud);
                mark = new GMarkerGoogle(pos, GMarkerGoogleType.red_small);
                markerOverlay.Markers.Add(mark);
                
            }

            cmdCargar.Enabled = true;
            cmdCargar.Text = "Cargar puntos";

            MessageBox.Show("Los focos de calor de cargaron correctamente!");



        }

        private void cmdGuardar_Click(object sender, EventArgs e) {
            cmdGuardar.Enabled = false;
            cmdGuardar.Text = "Guardando...";

            Ftp ftpClient = new Ftp(@"ftp://nrt3.modaps.eosdis.nasa.gov", "gerlamberti", "German95");
            string[] simpleDirectoryListing = ftpClient.directoryListDetailed("/FIRMS/viirs/Global/");


            string fileName = "";


            for (int i = 0; i < simpleDirectoryListing.Count(); i++) {
                string dir = simpleDirectoryListing[i];


                int index = dir.IndexOf("VIIRS");


                DateTime d = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
                string fechaJulianaToday = DateTime.Today.Year + d.DayOfYear.ToString("000.##");

                int posJuliano = dir.IndexOf(".txt") - 7;

                string fechaJulianaArchivo = "";

                if (posJuliano > 0) fechaJulianaArchivo = dir.Substring(posJuliano, 7);




                if (index >= 0 && fechaJulianaToday == fechaJulianaArchivo) {
                    fileName = dir.Substring(index);
                }
            }

            ftpClient.download("/FIRMS/viirs/Global/" + fileName, fileName);

            grabarPuntos(fileName);

            cmdGuardar.Enabled = true;
            cmdGuardar.Text = "Descargar puntos";

            MessageBox.Show("Los focos de calor se descargaron y guadaron correctamente!");

        }

        private void lblInfo_Click(object sender, EventArgs e) {
            MessageBox.Show("Safe Fire v1.0 - Nasa Space Apps Challenger Córdoba\n\tGermán Lamberti\n\tFlor Sanchez\n\tAgus Martínez\n\tOcta Sosa\n\tFranco Llamas", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
