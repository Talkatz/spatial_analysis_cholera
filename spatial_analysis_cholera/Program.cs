using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Data.OleDb;
using Microsoft.VisualBasic.CompilerServices;

namespace spatial_analysis_cholera
{
	class Program
	{
		static void Main(string[] args) 
		{
			// importing the data from excel into dataTables
			string pathPumps = Path.Combine(Directory.GetCurrentDirectory(), "pumpsCsv.csv"); ;
			string pathAddresses = Path.Combine(Directory.GetCurrentDirectory(), "addressesCsv.csv"); ;
			DataTable pumpsDt = ConvertCsvToDataTable(pathPumps, 3, ",");
			pumpsDt.TableName = "pumpsDt";
			DataTable addressesDt = ConvertCsvToDataTable(pathAddresses, 6, ",");
			pumpsDt.TableName = "addressesDt";

			// creating a list of points from the coordinates
			List<Point> pumpsPointsList = new List<Point>();
			for (int i = 0; i < pumpsDt.Rows.Count; i++)
			{
				Point pnt = new Point(double.Parse(pumpsDt.Rows[i]["xPump"].ToString()), double.Parse(pumpsDt.Rows[i]["yPump"].ToString()));
				pumpsPointsList.Add(pnt);
			}

			List<Point> choleraAddressesPointsList = new List<Point>();
			addressesDt.Columns.Add("infectedPerc", typeof(double)); // adding a column of the percentage of the cholera cases in the building
			for (int i = 0; i < addressesDt.Rows.Count; i++)
			{
				double residents = double.Parse(addressesDt.Rows[i]["buildingPop"].ToString());
				double numOfCholeraCases = double.Parse(addressesDt.Rows[i]["choleraCases"].ToString());
				addressesDt.Rows[i]["infectedPerc"] = numOfCholeraCases / residents;
				if ((numOfCholeraCases / residents) >= 0.05) //if at least 5% of the residents in the building were diagnosed with cholera
				{
					Point pnt = new Point(double.Parse(addressesDt.Rows[i]["x"].ToString()), double.Parse(addressesDt.Rows[i]["y"].ToString()));
					choleraAddressesPointsList.Add(pnt);
				}
			}

			// Analysis

			// now we'll check which pump is the nearest to the addresses of the main cholera cases
			pumpsDt.Columns.Add("avgDistToCholeraCases", typeof(double));
			for (int i = 0; i < pumpsDt.Rows.Count; i++)
			{
				double x = double.Parse(pumpsDt.Rows[i]["xPump"].ToString());
				double y = double.Parse(pumpsDt.Rows[i]["yPump"].ToString());
				pumpsDt.Rows[i]["avgDistToCholeraCases"] = Point.distanceFromPump(x, y, choleraAddressesPointsList);
				string pumpRow = String.Format("Pump number {0} average distance to the cholera cases is {1} meter.",
					pumpsDt.Rows[i]["pumpId"].ToString(), String.Format("{0:0}", pumpsDt.Rows[i]["avgDistToCholeraCases"]));
				Console.WriteLine(pumpRow);
			}
			// creating a new sorted table, in which the pumps on top will be those the smallest distance average, and that are needed accordingly to be checked first
			DataView dv = pumpsDt.DefaultView;
			dv.Sort = "avgDistToCholeraCases ASC";
			DataTable sortedPumpsDt = dv.ToTable();

			/* 
			  let's check the distance between the pump and the cholera cases from the cases point of view.
			   we'll calculate the mean centre  and weighted mean centre of the cases, and then check if these centres
			   are similar to the pump's average distance from the cases. The simple mean centre should be the same, 
			   but the weighted can be different. The weight is the percentage of the cholera cases in the building.
			   Soon to be added - Manhatten Median
			*/
			Point meanCenterPnt = Point.meanCentre(choleraAddressesPointsList);
			Point weightedMeanCenterPnt = Point.weightedMeanCentre(choleraAddressesPointsList, addressesDt);
			for (int i = 0; i < pumpsDt.Rows.Count; i++)
			{
				double x = double.Parse(pumpsDt.Rows[i]["xPump"].ToString()); //pumpsDt.Rows[i]["avgDistToCholeraCases"].ToString());
				double y = double.Parse(pumpsDt.Rows[i]["yPump"].ToString());
				Point pntPump = new Point(x, y);
				double distToMeanCenter = Point.returnPointsDistance(pntPump, meanCenterPnt);
				double distToWeightMeanCenter = Point.returnPointsDistance(pntPump, weightedMeanCenterPnt);

				string pumpRow = String.Format("Pump number {0} distance to the cholera cases mean center is {1} meter.",
					pumpsDt.Rows[i]["pumpId"].ToString(), String.Format("{0:0}", distToMeanCenter));
				string pumpRow2 = String.Format("Pump number {0} distance to the cholera cases weighted mean center is {1} meter.",
					pumpsDt.Rows[i]["pumpId"].ToString(), String.Format("{0:0}", distToWeightMeanCenter));

				Console.WriteLine(pumpRow);
				Console.WriteLine(pumpRow2);
			}

			// using an implementation I did for the 'Nearest Neighbour Index' to see if the cholera cases are clustered or dispersed
			double rectangleAreaOfCholeraCases = Point.rectangleArea(choleraAddressesPointsList);
			double nearestNeighborIndex = Point.nearestNeighbourIndex(choleraAddressesPointsList, rectangleAreaOfCholeraCases);
			if (nearestNeighborIndex < 1)
			{
				Console.WriteLine("The points pattern is clustered.");
			}
			else if (nearestNeighborIndex >= 1)
			{
				Console.WriteLine("The points pattern is randomly dispersed.");
			}
			else if ((nearestNeighborIndex > 1) && (nearestNeighborIndex < 2.15))
			{
				Console.WriteLine("The points pattern is regularly dispersed.");
			}
			else if (nearestNeighborIndex >= 2.15)
			{
				Console.WriteLine("The points pattern is regularly uniform dispersed.");
			}


			// now we will check if being near each pump is with correlation (linear regression) of being infected with cholera
			for (int i = 0; i < pumpsDt.Rows.Count; i++)
			{
				// making the two lists that will be checked with linear regression
				List<double> distances = new List<double>();
				List<double> infectedPer = new List<double>();
				Point pntPump = new Point(double.Parse(pumpsDt.Rows[i]["xPump"].ToString()), double.Parse(pumpsDt.Rows[i]["yPump"].ToString()));
				for (int j = 0; j < addressesDt.Rows.Count; j++)
				{
					Point pntAddr = new Point(double.Parse(addressesDt.Rows[j]["x"].ToString()), double.Parse(addressesDt.Rows[j]["y"].ToString()));
					distances.Add(Point.returnPointsDistance(pntAddr, pntPump));
					infectedPer.Add(double.Parse(addressesDt.Rows[j]["infectedPerc"].ToString()));
				}

				double correl = 0;
				double cov = 0;
				linearReg(distances, infectedPer, out correl, out cov);

				if (correl != (-999))
				{
					string pumpRow = String.Format("The correlation between the infected percentage in the buildings (The dependent) and their distances (The explanatory) " +
						"to pump number {0} is {1}.",
						pumpsDt.Rows[i]["pumpId"].ToString(), String.Format("{0:0.00}", correl));
					Console.WriteLine(pumpRow);
				}
			}
		} 

		// a method to import an csv into a dataTable
		public static DataTable ConvertCsvToDataTable(string csvFilePath, int numOfColumns, string delimiter)
		{
			DataTable dt = new DataTable();

			using (var reader = new StreamReader(csvFilePath))
			{
				int count = 0;
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					var values = line.Split(delimiter);
					
					if (count == 0) //header
					{
						for (int i =0; i < numOfColumns; i++)
						{
							string colName = values[i];
							dt.Columns.Add(colName, typeof(String));
						}
					}
					else //values
					{
						DataRow rowDt = dt.NewRow();
						for (int i = 0; i < numOfColumns; i++)
						{
							rowDt[i] = values[i];
						}
						dt.Rows.Add(rowDt);
					}
					count++;
				}
			}
			return dt;
		}

		// linear regression method
		public static void linearReg (List<double> list1, List<double> list2, out double correlation, out double covariance)
		{
			correlation = -999;
			covariance = -999;

			if (list1.Count != list2.Count)
			{
				Console.WriteLine("The lists must be of the same length");
				return;
			}

			double sumXy = 0;
			double sumX = 0;
			double sumXsqr = 0;
			double sumY = 0;
			double sumYsqr = 0;

			for (int i = 0; i < list1.Count; ++i)
			{
				Double x = list1[i];
				Double y = list2[i];

				sumX += x;
				sumXsqr += Math.Pow(x, 2.00);
				sumY += y;
				sumYsqr += Math.Pow(y, 2.00);

				sumXy += x * y;
			}

			try
			{
				correlation = (list1.Count * sumXy - sumX * sumY) / (Math.Sqrt((list1.Count * sumXsqr - Math.Pow(sumX, 2.00))
				   * (list1.Count * sumYsqr - Math.Pow(sumY, 2.00))));
				covariance = (sumXy / list1.Count - sumX * sumY / list1.Count / list1.Count);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}


			return;
		}
	}
}
