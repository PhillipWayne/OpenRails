﻿// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

/// 
/// Additional Contributions
/// Copyright (c) Jijun Tang
/// Can only be used by the Open Rails Project.
/// The message protocol defined in this file cannot be used in any software without specific written permission from admin@openrails.org.
/// This file cannot be copied, modified or included in any software which is not distributed directly by the Open Rails project.
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;
using ORTS;
using MSTS;
namespace ORTS.MultiPlayer
{
	public class Message
	{
		public string msg;
		public static Message Decode(string m)
		{
			int index = m.IndexOf(' ');
			string key = m.Substring(0, index);
			if (key == "MOVE") return new MSGMove(m.Substring(index + 1));
			else if (key == "SWITCHSTATES") return new MSGSwitchStatus(m.Substring(index + 1));
			else if (key == "SIGNALSTATES") return new MSGSignalStatus(m.Substring(index + 1));
			else if (key == "TEXT") return new MSGText(m.Substring(index + 1));
			else if (key == "LOCOINFO") return new MSGLocoInfo(m.Substring(index + 1));
			else if (key == "ALIVE") return new MSGAlive(m.Substring(index + 1));
			else if (key == "TRAIN") return new MSGTrain(m.Substring(index + 1));
			else if (key == "PLAYER") return new MSGPlayer(m.Substring(index + 1));
			else if (key == "ORGSWITCH") return new MSGOrgSwitch(m.Substring(index + 1));
			else if (key == "SWITCH") return new MSGSwitch(m.Substring(index + 1));
			else if (key == "RESETSIGNAL") return new MSGResetSignal(m.Substring(index + 1));
			else if (key == "REMOVETRAIN") return new MSGRemoveTrain(m.Substring(index + 1));
			else if (key == "SERVER") return new MSGServer(m.Substring(index + 1));
			else if (key == "MESSAGE") return new MSGMessage(m.Substring(index + 1));
			else if (key == "EVENT") return new MSGEvent(m.Substring(index + 1));
			else if (key == "UNCOUPLE") return new MSGUncouple(m.Substring(index + 1));
			else if (key == "COUPLE") return new MSGCouple(m.Substring(index + 1));
			else if (key == "GETTRAIN") return new MSGGetTrain(m.Substring(index + 1));
			else if (key == "UPDATETRAIN") return new MSGUpdateTrain(m.Substring(index + 1));
			else if (key == "CONTROL") return new MSGControl(m.Substring(index + 1));
			else if (key == "LOCCHANGE") return new MSGLocoChange(m.Substring(index + 1));
			else if (key == "QUIT") return new MSGQuit(m.Substring(index + 1));
			else if (key == "AVATAR") return new MSGAvatar(m.Substring(index + 1));
			else if (key == "WEATHER") return new MSGWeather(m.Substring(index + 1));
			else if (key == "AIDER") return new MSGAider(m.Substring(index + 1));
			else throw new Exception("Unknown Keyword" + key);
		}

		public virtual void HandleMsg() { System.Console.WriteLine("test"); return; }
	}

	#region MSGMove
	public class MSGMove : Message
	{
		class MSGMoveItem
		{
			public string user;
			public float speed;
			public float travelled;
			public int num, count;
			public int TileX, TileZ, trackNodeIndex, direction, tdbDir;
			public float X, Z;
			public MSGMoveItem(string u, float s, float t, int n, int tX, int tZ, float x, float z, int tni, int cnt, int dir, int tDir)
			{
				user = u; speed = s; travelled = t; num = n; TileX = tX; TileZ = tZ; X = x; Z = z; trackNodeIndex = tni; count = cnt; direction = dir; tdbDir = tDir;
			}
			public override string ToString()
			{
				return user + " " + speed + " " + travelled + " " + num + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + trackNodeIndex + " " + count + " " + direction + " " + tdbDir;
			}
		}
		List<MSGMoveItem> items;
		public MSGMove(string m)
		{
			m = m.Trim();
			string[] areas = m.Split(' ');
			if (areas.Length%12  != 0) //user speed travelled
			{
				throw new Exception("Parsing error " + m);
			}
			try
			{
				int i = 0;
				items = new List<MSGMoveItem>();
				for (i = 0; i < areas.Length / 12; i++)
					items.Add(new MSGMoveItem(areas[12 * i], float.Parse(areas[12 * i + 1]), float.Parse(areas[12 * i + 2]), int.Parse(areas[12 * i + 3]), int.Parse(areas[12 * i + 4]), int.Parse(areas[12 * i + 5]), float.Parse(areas[12 * i + 6]), float.Parse(areas[12 * i + 7]), int.Parse(areas[12 * i + 8]), int.Parse(areas[12 * i + 9]), int.Parse(areas[12 * i + 10]), int.Parse(areas[12*i+11])));
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		static Dictionary<int, int> MissingTimes;

		//a train is missing, but will wait for 10 messages then ask
		static bool CheckMissingTimes(int TNumber)
		{
			if (MissingTimes == null) MissingTimes = new Dictionary<int, int>();
			try
			{
				if (MissingTimes[TNumber] < 10) { MissingTimes[TNumber]++; return false; }
				else { MissingTimes[TNumber] = 0; return true; }
			}
			catch (Exception)
			{
				MissingTimes.Add(TNumber, 1);
				return false;
			}

		}

		public MSGMove()
		{
		}

		public void AddNewItem(string u, Train t)
		{
			if (items == null) items = new List<MSGMoveItem>();
			items.Add(new MSGMoveItem(u, t.SpeedMpS, t.travelled, t.Number, t.RearTDBTraveller.TileX, t.RearTDBTraveller.TileZ, t.RearTDBTraveller.X, t.RearTDBTraveller.Z, t.RearTDBTraveller.TrackNodeIndex, t.Cars.Count, (int)t.MUDirection, (int)t.RearTDBTraveller.Direction));
			t.LastReportedSpeed = t.SpeedMpS;
		}

		public bool OKtoSend()
		{
			if (items != null && items.Count > 0) return true;
			return false;
		}
		public override string ToString()
		{
			string tmp = "MOVE ";
			for (var i = 0; i < items.Count; i++) tmp += items[i].ToString() + " ";
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			foreach (MSGMoveItem m in items)
			{
				bool found = false; //a train may not be in my sim
				if (m.user == MPManager.GetUserName())//about itself, check if the number of car has changed, otherwise ignore
				{
					//if I am a remote controlled train now
					if (Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE)
					{
						Program.Simulator.PlayerLocomotive.Train.ToDoUpdate(m.trackNodeIndex, m.TileX, m.TileZ, m.X, m.Z, m.travelled, m.speed, m.direction, m.tdbDir);
					}
					found = true;/*
					try
					{
						if (m.count != Program.Simulator.PlayerLocomotive.Train.Cars.Count)
						{
							if (!MPManager.IsServer() && CheckMissingTimes(Program.Simulator.PlayerLocomotive.Train.Number)) MPManager.SendToServer((new MSGGetTrain(MPManager.GetUserName(), Program.Simulator.PlayerLocomotive.Train.Number)).ToString());
						}
					}
					catch (Exception) { }*/
					continue; 
				}
				if (m.user.Contains("0xAI") || m.user.Contains("0xUC"))
				{
					foreach (Train t in Program.Simulator.Trains)
					{
						if (t.Number == m.num)
						{
							found = true;
							if (t.Cars.Count != m.count) //the number of cars are different, client will not update it, ask for new information
							{
								if (!MPManager.IsServer())
								{
									if (CheckMissingTimes(t.Number)) MPManager.SendToServer((new MSGGetTrain(MPManager.GetUserName(), t.Number)).ToString());
									continue;
								}
							}
							if (t.TrainType == Train.TRAINTYPE.REMOTE)
							{
								t.ToDoUpdate(m.trackNodeIndex, m.TileX, m.TileZ, m.X, m.Z, m.travelled, m.speed, m.direction, m.tdbDir);
								break;
							}
						}
					}
				}
				else
				{
					Train t = MPManager.Instance().FindPlayerTrain(m.user);
					if (t != null)
					{
							found = true;
							t.ToDoUpdate(m.trackNodeIndex, m.TileX, m.TileZ, m.X, m.Z, m.travelled, m.speed, m.direction, m.tdbDir);
					}
				}
				if (found == false) //I do not have the train, tell server to send it to me
				{
					if (!MPManager.IsServer() && CheckMissingTimes(m.num)) MPManager.SendToServer((new MSGGetTrain(MPManager.GetUserName(), m.num)).ToString());
				}
			}
		}
	}
	#endregion MSGMove

	#region MSGRequired
	public class MSGRequired : Message
	{

	}
	#endregion

	#region MSGPlayer
	public class MSGPlayer : MSGRequired
	{
		public string user = "";
		public string code = "";
		public int num; //train number
		public string con; //consist
		public string path; //path consist and path will always be double quoted
		public string route;
		public int dir; //direction
		public int TileX, TileZ;
		public float X, Z, Travelled;
		public double seconds;
		public int season, weather;
		public int pantofirst, pantosecond;
		public string leadingID;
		public string[] cars;
		public string[] ids;
		public int[] flipped; //if a wagon is engine
		public int[] lengths; //if a wagon is engine
		public string url;
		public int version = 0;
		public MSGPlayer() { }
		public MSGPlayer(string m)
		{
			string[] areas = m.Split('\r');
			if (areas.Length <= 6)
			{
				throw new Exception("Parsing error in MSGPlayer" + m);
			}
			try
			{
				var tmp = areas[0].Trim();
				string[] data = tmp.Split(' ');
				user = data[0];
				if (MPManager.IsServer() && !MPManager.Instance().AllowNewPlayer)//server does not want to have more people
				{
					MPManager.BroadCast((new MSGMessage(user, "Error", "The dispatcher does not want to add more player")).ToString());
					throw (new Exception("Not want to add new player"));
				}
				code = data[1];
				num = int.Parse(data[2]);
				TileX = int.Parse(data[3]);
				TileZ = int.Parse(data[4]);
				X = float.Parse(data[5]);
				Z = float.Parse(data[6]);
				Travelled = float.Parse(data[7]);
				seconds = double.Parse(data[8]);
				season = int.Parse(data[9]);
				weather = int.Parse(data[10]);
				pantofirst = int.Parse(data[11]);
				pantosecond = int.Parse(data[12]);
				//user = areas[0].Trim();
				con = areas[2].Trim();
				route = areas[3].Trim();
				path = areas[4].Trim();
				dir = int.Parse(areas[5].Trim());
				url = areas[6].Trim();
				ParseTrainCars(areas[7].Trim());
				leadingID = areas[1].Trim();
				int index = path.LastIndexOf("\\PATHS\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					path = path.Remove(0, index + 7);
				}
				index = con.LastIndexOf("\\CONSISTS\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					con = con.Remove(0, index + 10);
				}
				if (areas.Length == 9) { version = int.Parse(areas[8]); }
			}
			catch (Exception e)
			{
				throw e;
			}
		}


		private void ParseTrainCars(string m)
		{
			string[] areas = m.Split('\t');
			var numCars = areas.Length;
			cars = new string[numCars];//with an empty "" at end
			ids = new string[numCars];
			flipped = new int[numCars];
			lengths = new int[numCars];
			int index, last;
			for (var i = 0; i < numCars; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
				lengths[i] = int.Parse(carinfo[2]);
			}

		}
		public MSGPlayer(string n, string cd, string c, string p, Train t, int tn, string avatar)
		{
			url = avatar;
			route = Program.Simulator.RoutePathName;
			int index = p.LastIndexOf("\\PATHS\\", StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				p = p.Remove(0, index + 7);
			}
			index = c.LastIndexOf("\\CONSISTS\\", StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				c = c.Remove(0, index + 10);
			}
			user = n; code = cd; con = c; path = p;
			if (t != null)
			{
				dir = (int)t.RearTDBTraveller.Direction; num = tn; TileX = t.RearTDBTraveller.TileX;
				TileZ = t.RearTDBTraveller.TileZ; X = t.RearTDBTraveller.X; Z = t.RearTDBTraveller.Z; Travelled = t.travelled;
			}
			seconds = Program.Simulator.ClockTime; season = (int)Program.Simulator.Season; weather = (int)Program.Simulator.Weather;
			pantofirst = pantosecond = 0;
			MSTSWagon w = (MSTSWagon)Program.Simulator.PlayerLocomotive;
			if (w != null)
			{
				pantofirst = w.AftPanUp == true ? 1 : 0;
				pantosecond = w.FrontPanUp == true ? 1 : 0;
			}

			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			lengths = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].RealWagFilePath;
				ids[i] = t.Cars[i].CarID;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
				lengths[i] = (int)(t.Cars[i].Length);
			}
			if (t.LeadLocomotive != null) leadingID = t.LeadLocomotive.CarID;
			else leadingID = "NA";

			version = MPManager.Instance().version;
		}
		public override string ToString()
		{
			string tmp = "PLAYER " + user + " " + code + " " + num + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " " + seconds + " " + season + " " + weather + " " + pantofirst + " " + pantosecond + " \r" + leadingID + "\r" + con + "\r" + route + "\r" + path + "\r" + dir + "\r" + url + "\r";
			for (var i = 0; i < cars.Length; i++)
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\n" + lengths[i] + "\t";
			}

			tmp += "\r" + MPManager.Instance().version;
			return "" + tmp.Length + ": " + tmp;
		}

		private object lockObjPlayer = new object();
		public override void HandleMsg()
		{
			if (this.version != MPManager.Instance().version)
			{
				var reason = "Wrong version of protocol, please update to version " + MPManager.Instance().version;
				MPManager.BroadCast((new MSGMessage(this.user, "Error", reason)).ToString());//server will broadcast this error
				if (MPManager.IsServer()) throw new Exception("Wrong version of protocol, please update to version " + MPManager.Instance().version);//ignore this player message
				else
				{
					System.Console.WriteLine("Wrong version of protocol, will play in single mode, please update to version " + MPManager.Instance().version);
					throw new MultiPlayerError();//client, close the connection
				}
			}
			//check if other players with the same name is online
			if (MPManager.IsServer())
			{
				//if someone with the same name is there, will throw a fatal error
				lock (lockObjPlayer)
				{
					if (MPManager.Instance().FindPlayerTrain(user) != null || MPManager.GetUserName() == user)
					{
						MPManager.BroadCast((new MSGMessage(user, "Error", "A user with the same name exists")).ToString());
						//throw new MultiPlayerError();
					}
				}
			}
			lock (lockObjPlayer)
			{

				if (MPManager.Instance().FindPlayerTrain(user) != null) return; //already added the player, ignore
				MPManager.OnlineTrains.AddPlayers(this, null);

				//System.Console.WriteLine(this.ToString());
				if (MPManager.IsServer())// && Program.Server.IsRemoteServer())
				{
					MPManager.BroadCast((new MSGOrgSwitch(user, MPManager.Instance().OriginalSwitchState)).ToString());
					MPManager.Instance().PlayerAdded = true;
				}
				else //client needs to handle environment
				{
					if (MPManager.GetUserName() == this.user) //a reply from the server, update my train number
					{
						Program.Client.Connected = true;
						if (Program.Simulator.PlayerLocomotive == null) Program.Simulator.Trains[0].Number = this.num;
						else Program.Simulator.PlayerLocomotive.Train.Number = this.num;
					}
					Program.Simulator.Weather = (WeatherType)this.weather;
					Program.Simulator.ClockTime = this.seconds;
					Program.Simulator.Season = (SeasonType)this.season;
				}
			}
		}

		public void HandleMsg(OnlinePlayer p)
		{
			if (!MPManager.IsServer()) return; //only intended for the server, when it gets the player message in OnlinePlayer Receive
			if (this.version != MPManager.Instance().version)
			{
				var reason = "Wrong version of protocol, please update to version " + MPManager.Instance().version;
				MPManager.BroadCast((new MSGMessage(this.user, "Error", reason)).ToString());
				throw new Exception("Wrong version of protocol");
			}
			//check if other players with the same name is online
				//if someone with the same name is there, will throw a fatal error
			if (MPManager.Instance().FindPlayerTrain(user) != null || MPManager.GetUserName() == user)
			{
				MPManager.BroadCast((new MSGMessage(user, "Error", "A user with the same name exists")).ToString());
				throw new MultiPlayerError();
			}

			MPManager.OnlineTrains.AddPlayers(this, p);
			//System.Console.WriteLine(this.ToString());
			MPManager.BroadCast((new MSGOrgSwitch(user, MPManager.Instance().OriginalSwitchState)).ToString());
			MPManager.Instance().PlayerAdded = true;

			//System.Console.WriteLine(host.ToString() + Program.Simulator.OnlineTrains.AddAllPlayerTrain());

		}
	}

	#endregion MSGPlayer

	#region MGSwitch

	public class MSGSwitch : Message
	{
		public string user;
		public int TileX, TileZ, WorldID, Selection;
		public bool HandThrown;
		bool OK = true;

		public MSGSwitch(string m)
		{

			string[] tmp = m.Split(' ');
			if (tmp.Length != 6) throw new Exception("Parsing error " + m);
			user = tmp[0];
			TileX = int.Parse(tmp[1]);
			TileZ = int.Parse(tmp[2]);
			WorldID = int.Parse(tmp[3]);
			Selection = int.Parse(tmp[4]);
			HandThrown = bool.Parse(tmp[5]);
		}

		public MSGSwitch(string n, int tX, int tZ, int u, int s, bool handThrown)
		{
			if (MPManager.Instance().TrySwitch == false)
			{
				if (handThrown && Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Information("Dispatcher does not allow hand throw at this time");
				OK = false;
				return;
			}
			user = n;
			WorldID = u;
			TileX = tX;
			TileZ = tZ;
			Selection = s;
			HandThrown = handThrown;
		}

		public override string ToString()
		{
			if (!OK) return null;
			string tmp = "SWITCH " + user + " " + TileX + " " + TileZ + " " + WorldID + " " + Selection + " " + HandThrown;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			//System.Console.WriteLine(this.ToString());
			if (MPManager.IsServer()) //server got this message from Client
			{
				//if a normal user, and the dispatcher does not want hand throw, just ignore it
				if (HandThrown == true && !MPManager.Instance().AllowedManualSwitch && !MPManager.Instance().aiderList.Contains(user))
				{
					MPManager.BroadCast((new MSGMessage(user, "SwitchWarning", "Server does not allow hand thrown of switch")).ToString());
					return;
				}
				TrJunctionNode trj = Program.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, WorldID);
				if (Program.Simulator.SwitchIsOccupied(trj))
				{
					MPManager.BroadCast((new MSGMessage(user, "Warning", "Train on the switch, cannot throw")).ToString());
					return;
				}
				trj.SelectedRoute = Selection;
				MPManager.BroadCast(this.ToString()); //server will tell others
			}
			else
			{
				TrJunctionNode trj = Program.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, WorldID);
				trj.SelectedRoute = Selection;
				if (user == MPManager.GetUserName() && HandThrown == true)//got the message with my name, will confirm with the player
				{
					Program.Simulator.Confirmer.Information("Switched, current route is " + (Selection == 0? "main":"side") + " route");
					return;
				}
			}
		}
	}

	#endregion MGSwitch

	#region MSGResetSignal
	public class MSGResetSignal : Message
	{
		public string user;
		public int TileX, TileZ, WorldID, Selection;

		public MSGResetSignal(string m)
		{
			user = m.Trim();
		}

		public override string ToString()
		{
			string tmp = "RESETSIGNAL " + user;
			//System.Console.WriteLine(tmp);
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (MPManager.IsServer() && MPManager.Instance().AllowedManualSwitch)
			{
				try
				{
					var t = MPManager.Instance().FindPlayerTrain(user);
					if (t != null) t.ResetSignal();
					MultiPlayer.MPManager.BroadCast((new MSGSignalStatus()).ToString());
				}
				catch (Exception) { }
			}
		}
	}
	#endregion MSGResetSignal

	#region MSGOrgSwitch
	public class MSGOrgSwitch : MSGRequired
	{
		SortedList<uint, TrJunctionNode> SwitchState;
		public string msgx = "";
		string user = "";
		byte[] switchStatesArray;
		public MSGOrgSwitch(string u, string m)
		{
			user = u; msgx = m;
		}

		public MSGOrgSwitch(string m)
		{
			string[] tmp = m.Split('\t');
			user = tmp[0].Trim();
			byte[] gZipBuffer = Convert.FromBase64String(tmp[1]);
			using (var memoryStream = new MemoryStream())
			{
				int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
				memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

				switchStatesArray = new byte[dataLength];

				memoryStream.Position = 0;
				using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
				{
					gZipStream.Read(switchStatesArray, 0, switchStatesArray.Length);
				}
			}

		}

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer() || user != MPManager.GetUserName()) return; //server will ignore it
			uint key = 0;
			SwitchState = new SortedList<uint, TrJunctionNode>();
			try
			{
				foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
				{
					if (t != null && t.TrJunctionNode != null)
					{
						key = t.Index;
						SwitchState.Add(key, t.TrJunctionNode);
					}
				}
			}
			catch (Exception e) { SwitchState = null; throw e; } //if error, clean the list and wait for the next signal

			int i = 0, state = 0;
			foreach (System.Collections.Generic.KeyValuePair<uint, TrJunctionNode> t in SwitchState)
			{
				state = (int)switchStatesArray[i];
				if (t.Value.SelectedRoute != state)
				{
					t.Value.SelectedRoute = state;
				}
				i++;
			}

		}

		public override string ToString()
		{
			string tmp = "ORGSWITCH " + user + "\t" + msgx;
			return "" + tmp.Length + ": " + tmp;
		}
	}
	#endregion MSGOrgSwitch

	#region MSGSwitchStatus
	public class MSGSwitchStatus : Message
	{
		static byte[] preState;
		static SortedList<uint, TrJunctionNode> SwitchState;
		public bool OKtoSend = false;
		static byte[] switchStatesArray;
		public MSGSwitchStatus()
		{
			var i = 0;
			if (SwitchState == null)
			{
				SwitchState = new SortedList<uint, TrJunctionNode>();
				uint key = 0;
				foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
				{
					if (t != null && t.TrJunctionNode != null)
					{
						key = t.Index;
						SwitchState.Add(key, t.TrJunctionNode);
					}
				}
				switchStatesArray = new byte[SwitchState.Count() + 2];
			}
			if (preState == null)
			{
				preState = new byte[SwitchState.Count() + 2];
				for (i = 0; i < preState.Length; i++) preState[i] = 0;
			}
			i = 0;
			foreach (System.Collections.Generic.KeyValuePair<uint, TrJunctionNode> t in SwitchState)
			{
				switchStatesArray[i] = (byte)t.Value.SelectedRoute;
				i++;
			}
			OKtoSend = false;
			for (i = 0; i < SwitchState.Count; i++)
			{
				if (switchStatesArray[i] != preState[i]) { OKtoSend = true; }//something is different, will send
				preState[i] = switchStatesArray[i];
			}
			if (OKtoSend == false)
			{
				//new player added, will keep sending for a while
				if (Program.Simulator.GameTime - MPManager.Instance().lastPlayerAddedTime < 3 * MPManager.Instance().MPUpdateInterval) OKtoSend = true;
			}
		}

		public MSGSwitchStatus(string m)
		{
			if (SwitchState == null)
			{
				uint key = 0;
				SwitchState = new SortedList<uint, TrJunctionNode>();
				try
				{
					foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
					{
						if (t != null && t.TrJunctionNode != null)
						{
							key = t.Index;
							SwitchState.Add(key, t.TrJunctionNode);
						}
					}
					switchStatesArray = new byte[SwitchState.Count + 128];//a bit more for safety
				}
				catch (Exception e) { SwitchState = null; throw e; } //if error, clean the list and wait for the next signal

			}
			byte[] gZipBuffer = Convert.FromBase64String(m);
			using (var memoryStream = new MemoryStream())
			{
				int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
				memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

				memoryStream.Position = 0;
				using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
				{
					gZipStream.Read(switchStatesArray, 0, switchStatesArray.Length);
				}

			}
		}

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer()) return; //server will ignore it


			int i = 0, state = 0;
			foreach (System.Collections.Generic.KeyValuePair<uint, TrJunctionNode> t in SwitchState)
			{
				state = (int)switchStatesArray[i];
				if (t.Value.SelectedRoute != state)
				{
					if (!SwitchOccupiedByPlayerTrain(t.Value)) t.Value.SelectedRoute = state;
				}
				i++;
			}

		}

		private bool SwitchOccupiedByPlayerTrain(TrJunctionNode junctionNode)
		{
			if (Program.Simulator.PlayerLocomotive == null) return false;
			Train train = Program.Simulator.PlayerLocomotive.Train;
			if (train == null) return false;
			if (train.FrontTDBTraveller.TrackNodeIndex == train.RearTDBTraveller.TrackNodeIndex)
				return false;
			Traveller traveller = new Traveller(train.RearTDBTraveller);
			while (traveller.NextSection())
			{
				if (traveller.TrackNodeIndex == train.FrontTDBTraveller.TrackNodeIndex)
					break;
				if (traveller.TN.TrJunctionNode == junctionNode)
					return true;
			}
			return false;
		}

		public override string ToString()
		{
			byte[] buffer = switchStatesArray;
			var memoryStream = new MemoryStream();
			using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
			{
				gZipStream.Write(buffer, 0, buffer.Length);
			}

			memoryStream.Position = 0;

			var compressedData = new byte[memoryStream.Length];
			memoryStream.Read(compressedData, 0, compressedData.Length);

			var gZipBuffer = new byte[compressedData.Length + 4];
			Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);

			string tmp = "SWITCHSTATES " + Convert.ToBase64String(gZipBuffer);
			return "" + tmp.Length + ": " + tmp;
		}
	}
	#endregion MSGSwitchStatus
	#region MSGTrain
	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGTrain : Message
	{
		string[] cars;
		string[] ids;
		int[] flipped; //if a wagon is engine
		int[] lengths;
		int TrainNum;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		int mDirection;

		public MSGTrain(string m)
		{
			//System.Console.WriteLine(m);
			int index = m.IndexOf(' '); int last = 0;
			TrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			direction = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileX = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileZ = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			X = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Z = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Travelled = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			mDirection = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			string[] areas = m.Split('\t');
			cars = new string[areas.Length-1];//with an empty "" at end
			ids = new string[areas.Length - 1];
			flipped = new int[areas.Length - 1];
			lengths = new int[areas.Length - 1];
			for (var i = 0; i < cars.Length; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
				lengths[i] = int.Parse(carinfo[2]);
			}

			//System.Console.WriteLine(this.ToString());

		}

		public MSGTrain(Train t, int n)
		{
			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			lengths = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].RealWagFilePath;
				ids[i] = t.Cars[i].CarID;
				lengths[i] = (int)t.Cars[i].Length;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}
			TrainNum = n;
			direction = t.RearTDBTraveller.Direction==Traveller.TravellerDirection.Forward?1:0;
			TileX = t.RearTDBTraveller.TileX;
			TileZ = t.RearTDBTraveller.TileZ;
			X = t.RearTDBTraveller.X;
			Z = t.RearTDBTraveller.Z;
			Travelled = t.travelled;
			mDirection = (int)t.MUDirection;
		}

		private object lockObj = new object();

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer()) return; //server will ignore it
			//System.Console.WriteLine(this.ToString());
			// construct train data
			Train train = null; 
			train = new Train(Program.Simulator);
			train.Number = this.TrainNum;

			train.TrainType = Train.TRAINTYPE.REMOTE;
			int consistDirection = direction;
			train.travelled = Travelled;
			train.MUDirection = (Direction)this.mDirection;
			train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			//if (consistDirection != 1)
			//	train.RearTDBTraveller.ReverseDirection();
			TrainCar previousCar = null;
			for(var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				string wagonFilePath = Program.Simulator.BasePath + @"\trains\trainset\" + cars[i];
				TrainCar car = null;
				try
				{
					car = RollingStock.Load(Program.Simulator, wagonFilePath, previousCar);
					car.Length = lengths[i];
				}
				catch (Exception error)
				{
					System.Console.WriteLine( wagonFilePath +" " + error);
					car = MPManager.Instance().SubCar(wagonFilePath, lengths[i], previousCar);
				}
				if (car == null) continue;
				bool flip = true;
				if (flipped[i] == 0) flip = false;
				car.Flipped = flip;
				car.CarID = ids[i];
				train.Cars.Add(car);
				car.Train = train;
				previousCar = car;

			}// for each rail car

			if (train.Cars.Count == 0) return;

			train.CalculatePositionOfCars(0);
			train.InitializeBrakes();
			train.InitializeSignals(false);//client do it won't have impact
			train.CheckFreight();

			if (train.Cars[0] is MSTSLocomotive) train.LeadLocomotive = train.Cars[0];
			if (MPManager.Instance().AddOrRemoveTrain(train, true) == false) return; //add train, but failed
		}

		public override string ToString()
		{
			string tmp = "TRAIN " + TrainNum + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " " + mDirection + " ";
			for(var i = 0; i < cars.Length; i++) 
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\n" + lengths[i] + "\t";
			}
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGTrain

	#region MSGUpdateTrain

	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGUpdateTrain : Message
	{
		string[] cars;
		string[] ids;
		int[] flipped; //if a wagon is engine
		int[] lengths; //if a wagon is engine
		int TrainNum;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		int mDirection;
		string user;
		public MSGUpdateTrain(string m)
		{
			//System.Console.WriteLine(m);
			int index = m.IndexOf(' '); int last = 0;
			user = m.Substring(0, index + 1);
			m = m.Remove(0, index + 1);
			user = user.Trim();

			index = m.IndexOf(' ');
			TrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			direction = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileX = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileZ = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			X = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Z = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Travelled = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			mDirection = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			string[] areas = m.Split('\t');
			cars = new string[areas.Length - 1];//with an empty "" at end
			ids = new string[areas.Length - 1];
			flipped = new int[areas.Length - 1];
			lengths = new int[areas.Length - 1];
			for (var i = 0; i < cars.Length; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
				lengths[i] = int.Parse(carinfo[2]);
			}

			//System.Console.WriteLine(this.ToString());

		}
		public MSGUpdateTrain(string u, Train t, int n)
		{
			user = u;
			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].RealWagFilePath;
				ids[i] = t.Cars[i].CarID;
				lengths[i] = (int)t.Cars[i].Length;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}
			TrainNum = n;
			direction = t.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 1 : 0;
			TileX = t.RearTDBTraveller.TileX;
			TileZ = t.RearTDBTraveller.TileZ;
			X = t.RearTDBTraveller.X;
			Z = t.RearTDBTraveller.Z;
			Travelled = t.travelled;
			mDirection = (int) t.MUDirection;
		}

		TrainCar findCar(Train t, string name)
		{
			foreach (TrainCar car in t.Cars)
			{
				if (car.CarID == name) return car;
			}
			return null;
		}
		private object lockObj = new object();

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer()) return; //server will ignore it
			if (user != MPManager.GetUserName()) return; //not the one requested GetTrain
			bool found = false; Train train1 = null; Train train = null;
			lock (lockObj)
			{
				// construct train data
				foreach (Train t in Program.Simulator.Trains)
				{
					if (t.Number == this.TrainNum) //the train exists, update information
					{
						train = t;
						found = true; break;
					}
				}
				if (!found)
				{
					//not found, create new train
					train1 = new Train(Program.Simulator); train1.Number = this.TrainNum; 
				}
			}
			if (found)
			{
				Traveller traveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
				TrainCar previousCar = null;
				List<TrainCar> tmpCars = new List<TrainCar>();
				for (var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
				{
					string wagonFilePath = Program.Simulator.BasePath + @"\trains\trainset\" + cars[i];
					TrainCar car = findCar(train, ids[i]);
					try
					{
						if (car == null) car = RollingStock.Load(Program.Simulator, wagonFilePath, previousCar);
						car.Length = lengths[i];
					}
					catch (Exception error)
					{
						System.Console.WriteLine(wagonFilePath + " " + error);
						car = MPManager.Instance().SubCar(wagonFilePath, lengths[i], previousCar);
					}
					if (car == null) continue;
					bool flip = true;
					if (flipped[i] == 0) flip = false;
					car.Flipped = flip;
					car.CarID = ids[i];
					tmpCars.Add(car);
					car.Train = train;
					previousCar = car;

				}// for each rail car

				if (tmpCars.Count == 0) return;

				train.Cars = tmpCars;
				train.MUDirection = (Direction)mDirection;
				train.RearTDBTraveller = traveller;
				train.CalculatePositionOfCars(0);
				train.travelled = Travelled;
				train.CheckFreight();
				return;
			}
			train1.TrainType = Train.TRAINTYPE.REMOTE;
			int consistDirection = direction;
			train1.travelled = Travelled;
			train1.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			TrainCar previousCar1 = null;
			for (var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				string wagonFilePath = Program.Simulator.BasePath + @"\trains\trainset\" + cars[i];
				TrainCar car = null;
				try
				{
					car = RollingStock.Load(Program.Simulator, wagonFilePath, previousCar1);
					car.Length = lengths[i];
				}
				catch (Exception error)
				{
					System.Console.WriteLine(wagonFilePath + " " + error);
					car = MPManager.Instance().SubCar(wagonFilePath, lengths[i], previousCar1);
				}
				if (car == null) continue;
				bool flip = true;
				if (flipped[i] == 0) flip = false;
				car.Flipped = flip;
				car.CarID = ids[i];
				train1.Cars.Add(car);
				car.Train = train1;
				previousCar1 = car;
			}// for each rail car

			if (train1.Cars.Count == 0) return;
			train1.MUDirection = (Direction)mDirection;
			train1.CalculatePositionOfCars(0);
			train1.InitializeBrakes();
			train1.InitializeSignals(false);
			train1.CheckFreight();

			if (train1.Cars[0] is MSTSLocomotive) train1.LeadLocomotive = train1.Cars[0];
			MPManager.Instance().AddOrRemoveTrain(train1, true);
		}

		public override string ToString()
		{
			string tmp = "UPDATETRAIN " + user + " " + TrainNum + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " " + mDirection + " ";
			for (var i = 0; i < cars.Length; i++)
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\n" + lengths[i] + "\t";
			}
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGUpdateTrain

	#region MSGRemoveTrain
	//remove AI trains
	public class MSGRemoveTrain : Message
	{
		public List<int> trains;

		public MSGRemoveTrain(string m)
		{
			string[] tmp = m.Split(' ');
			trains = new List<int>();
			for (var i = 0; i < tmp.Length; i++)
			{
				trains.Add(int.Parse(tmp[i]));
			}
		}

		public MSGRemoveTrain(List<Train> ts)
		{
			trains = new List<int>();
			foreach (Train t in ts)
			{
				trains.Add(t.Number);
			}
		}

		public override string ToString()
		{

			string tmp = "REMOVETRAIN";
			foreach (int i in trains)
			{
				tmp += " " + i;
			}
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			foreach (int i in trains)
			{
				foreach (Train train in Program.Simulator.Trains)
				{
					if (i == train.Number)
					{
						MPManager.Instance().AddOrRemoveTrain(train, false);//added to the removed list, treated later to be thread safe
					}
				}
			}
		}

	}

	#endregion MSGRemoveTrain

	#region MSGServer
	public class MSGServer : MSGRequired
	{
		string user; //true: I am a server now, false, not
		public MSGServer(string m)
		{
			user = m.Trim();
		}


		public override string ToString()
		{
			string tmp = "SERVER " + user;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (MPManager.GetUserName() == user || user == "YOU")
			{
				if (Program.Server != null) return; //already a server, not need to worry
				Program.Client.Connected = true;
				MPManager.Instance().NotServer = false;
				MPManager.Instance().RememberOriginalSwitchState();
				System.Console.WriteLine("You are the new dispatcher, enjoy");
				if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Information("You are the new dispatcher, enjoy");
				//System.Console.WriteLine(this.ToString());
			}
			else
			{
				MPManager.Instance().NotServer = true;
				if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Information("New dispatcher is " + user);
				System.Console.WriteLine("New dispatcher is " + user);
			}
		}
	}
	#endregion MSGServer

	#region MSGAlive
	public class MSGAlive : Message
	{
		string user;
		public MSGAlive(string m)
		{
			user = m;
		}


		public override string ToString()
		{
			string tmp = "ALIVE " + user;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			//nothing to worry at this stage
			//System.Console.WriteLine(this.ToString());
		}
	}
	#endregion MSGAlive

	#region MSGTrainMerge
	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGTrainMerge : Message
	{
		int TrainNumRetain;
		int TrainNumRemoved;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		public MSGTrainMerge(string m)
		{
			m = m.Trim();
			string[] areas = m.Split(' ');
			TrainNumRetain = int.Parse(areas[0]);
			TrainNumRemoved = int.Parse(areas[1]);
			direction = int.Parse(areas[2]);
			TileX = int.Parse(areas[3]);
			TileZ = int.Parse(areas[4]);
			X = float.Parse(areas[5]);
			Z = float.Parse(areas[6]);
			Travelled = float.Parse(areas[7]);
		}
		public MSGTrainMerge(Train t1, Train t2)
		{
			TrainNumRetain = t1.Number;
			TrainNumRemoved = t2.Number;
			direction = t1.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 1 : 0; 
			TileX = t1.RearTDBTraveller.TileX;
			TileZ = t1.RearTDBTraveller.TileZ;
			X = t1.RearTDBTraveller.X;
			Z = t1.RearTDBTraveller.Z;
			Travelled = t1.travelled;

		}

		public override void HandleMsg() 
		{

		}

		public override string ToString()
		{
			string tmp = "TRAINMERGE " + TrainNumRetain + " " + TrainNumRemoved + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled;
			return "" + tmp.Length + ": " + tmp;
		}
	}
	#endregion MSGTrainMerge

	#region MSGMessage
	//warning, error or information from the server, a client receives Error will disconnect itself
	public class MSGMessage : MSGRequired
	{
		string msgx;
		string level;
		string user; 
		public MSGMessage(string m)
		{
			string[] t = m.Split('\t');
			user = t[0].Trim();
			level = t[1].Trim();
			msgx = t[2];

		}

		public MSGMessage(string u, string l, string m)
		{
			user = u;
			level = l;

			msgx = m;
		}

		public override void HandleMsg()
		{
			if (MPManager.GetUserName() == user || user == "All")
			{
				if (Program.Simulator.Confirmer != null)
					Program.Simulator.Confirmer.Message(level == "Error" ? ConfirmLevel.Error : level == "Warning" ? ConfirmLevel.Warning : level == "Info" ? ConfirmLevel.Information : ConfirmLevel.None, msgx);
				System.Console.WriteLine(level + ": " + msgx); 
				if (level == "Error" && !MPManager.IsServer())//if is a client, fatal error, will close the connection, and get into single mode
				{
					MPManager.Notify((new MSGQuit(MPManager.GetUserName())).ToString());//to be nice, still send a quit before close the connection
					throw new MultiPlayerError();//this is a fatal error, thus the client will be stopped in ClientComm
				}
				else if (level == "SwitchWarning")
				{
					MPManager.Instance().TrySwitch = false;
					return;
				}
				else if (level == "SwitchOK")
				{
					MPManager.Instance().TrySwitch = true;
					return;
				}
			}
		}

		public override string ToString()
		{
			string tmp = "MESSAGE " + user + "\t" + level + "\t" + msgx;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGMessage

	#region MSGControl
	//message to ask for the control of a train or confirm it
	public class MSGControl : Message
	{
		int num;
		string level;
		string user;
		public MSGControl(string m)
		{
			m.Trim();
			string[] t = m.Split('\t');
			user = t[0];
			level = t[1];
			num = int.Parse(t[2]);
		}

		public MSGControl(string u, string l, Train t)
		{
			user = u;
			level = l;
			num = t.Number;
		}

		public override void HandleMsg()
		{
			if (MPManager.GetUserName() == user && level == "Confirm")
			{
				Train train = Program.Simulator.PlayerLocomotive.Train;
				train.TrainType = Train.TRAINTYPE.PLAYER; train.LeadLocomotive = Program.Simulator.PlayerLocomotive;
				if (Program.Simulator.Confirmer != null)
					Program.Simulator.Confirmer.Information("You gained back the control of your train");
				MPManager.Instance().RemoveUncoupledTrains(train);
			}
			else if (level == "Confirm") //server inform me that a train is now remote
			{
				foreach (var p in MPManager.OnlineTrains.Players)
				{
					if (p.Key == user) {
						foreach (var t in Program.Simulator.Trains)
						{
							if (t.Number == this.num) p.Value.Train = t;
						}
						MPManager.Instance().RemoveUncoupledTrains(p.Value.Train);
						p.Value.Train.TrainType = Train.TRAINTYPE.REMOTE;
						break;
					}
				}
			}
			else if (MPManager.IsServer() && level == "Request")
			{
				foreach (var p in MPManager.OnlineTrains.Players)
				{
					if (p.Key == user)
					{
						foreach (var t in Program.Simulator.Trains)
						{
							if (t.Number == this.num) p.Value.Train = t;
						}
						p.Value.Train.TrainType = Train.TRAINTYPE.REMOTE;
						MPManager.Instance().RemoveUncoupledTrains(p.Value.Train);
						MPManager.BroadCast((new MSGControl(user, "Confirm", p.Value.Train)).ToString());
						break;
					}
				}
			}
		}

		public override string ToString()
		{
			string tmp = "CONTROL " + user + "\t" + level + "\t" + num;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGControl

	#region MSGLocoChange
	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGLocoChange : Message
	{
		int num;
		string engine;
		string user;
		public MSGLocoChange(string m)
		{
			m.Trim();
			string[] t = m.Split('\t');
			user = t[0];
			engine = t[1];
			num = int.Parse(t[2]);
		}

		public MSGLocoChange(string u, string l, Train t)
		{
			user = u;
			engine = l;
			num = t.Number;
		}

		public override void HandleMsg()
		{
			foreach (var t in Program.Simulator.Trains)
			{
				foreach (var car in t.Cars)
				{
					if (car.CarID == engine)
					{
						car.Train.LeadLocomotive = car;
						foreach (var p in MPManager.OnlineTrains.Players)
						{
							if (p.Value.Train == t) { p.Value.LeadingLocomotiveID = car.CarID; break; }
						}
						if (MPManager.IsServer()) MPManager.BroadCast((new MSGLocoChange(user, engine, t)).ToString());
						return;
					}
				}
			}
		}

		public override string ToString()
		{
			string tmp = "LOCCHANGE " + user + "\t" + engine + "\t" + num;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGLocoChange

	#region MSGEvent
	public class MSGEvent : Message
	{
		public string user;
		public string EventName;
		public int EventState;

		public MSGEvent(string m)
		{
			string[] tmp = m.Split(' '); 
			if (tmp.Length != 3) throw new Exception("Parsing error " + m);
			user = tmp[0].Trim();
			EventName = tmp[1].Trim();
			EventState = int.Parse(tmp[2]);
		}

		public MSGEvent(string m, string e, int ID)
		{
			user = m.Trim();
			EventName = e;
			EventState = ID;
		}

		public override string ToString()
		{

			string tmp = "EVENT " + user + " " + EventName + " " + EventState;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) return; //avoid myself
			Train t = MPManager.Instance().FindPlayerTrain(user);
			if (t == null) return;

			if (EventName == "HORN")
			{
				t.SignalEvent(EventState);
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "PANTO2")
			{
				MSTSWagon w = (MSTSWagon)t.Cars[0];
				if (w == null) return;

				w.FrontPanUp = (EventState == 1 ? true : false);

				foreach (TrainCar car in t.Cars)
					if (car is MSTSWagon) ((MSTSWagon)car).FrontPanUp = w.FrontPanUp;
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "PANTO1")
			{
				MSTSWagon w = (MSTSWagon)t.Cars[0];
				if (w == null) return;

				w.AftPanUp = (EventState == 1 ? true : false);

				foreach (TrainCar car in t.Cars)
					if (car is MSTSWagon) ((MSTSWagon)car).AftPanUp = w.AftPanUp;
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "BELL")
			{
				if (t.LeadLocomotive != null) t.LeadLocomotive.SignalEvent(EventState == 0 ? EventID.BellOff : EventID.BellOn);
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "WIPER")
			{
				if (t.LeadLocomotive != null) t.LeadLocomotive.SignalEvent(EventState == 0 ? EventID.WiperOff : EventID.WiperOn);
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "HEADLIGHT")
			{
				if (t.LeadLocomotive != null && EventState == 0) t.LeadLocomotive.SignalEvent(EventID.HeadlightOff);
				if (t.LeadLocomotive != null && EventState == 1) t.LeadLocomotive.SignalEvent(EventID.HeadlightDim);
				if (t.LeadLocomotive != null && EventState == 2) t.LeadLocomotive.SignalEvent(EventID.HeadlightOn);
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else return;
		}

	}

	#endregion MSGEvent

	#region MSGQuit
	public class MSGQuit : Message
	{
		public string user;

		public MSGQuit(string m)
		{
			user = m.Trim();
		}

		public override string ToString()
		{

			string tmp = "QUIT " + user;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) return; //avoid myself

			bool ServerQuit = false;
			if (Program.Client != null && user.Contains("ServerHasToQuit")) //the server quits, will send a message with ServerHasToQuit\tServerName
			{
				if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Error("Server quits, will play as single mode");
				user = user.Replace("ServerHasToQuit\t", ""); //get the user name of server from the message
				ServerQuit = true;
			}
			OnlinePlayer p = null;
			if (MPManager.OnlineTrains.Players.ContainsKey(user))
			{
				p = MPManager.OnlineTrains.Players[user];
			}
			if (p != null && Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Information(this.user + " quit.");
			if (MPManager.IsServer())
			{
				if (p != null)
				{
					//if the one quit controls my train, I will gain back the control
					if (p.Train == Program.Simulator.PlayerLocomotive.Train) 
						Program.Simulator.PlayerLocomotive.Train.TrainType = Train.TRAINTYPE.PLAYER;
					MPManager.Instance().AddRemovedPlayer(p);
				}
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else //client will remove train
			{
				if (p != null)
				{
					//if the one quit controls my train, I will gain back the control
					if (p.Train == Program.Simulator.PlayerLocomotive.Train)
						Program.Simulator.PlayerLocomotive.Train.TrainType = Train.TRAINTYPE.PLAYER;
					MPManager.Instance().AddRemovedPlayer(p);
					if (ServerQuit)//warning, need to remove other player trains if there are not AI, in the future
					{
						//no matter what, let player gain back the control of the player train
						Program.Simulator.PlayerLocomotive.Train.TrainType = Train.TRAINTYPE.PLAYER;
						throw new MultiPlayerError(); //server quit, end communication by throwing this error 
					}
				}
			}
		}

	}

	#endregion MSGQuit

	#region MSGGetTrain
	public class MSGGetTrain : Message
	{
		public int num;
		public string user;

		public MSGGetTrain(string u, int m)
		{
			user = u; num = m;
		}

		public MSGGetTrain(string m)
		{
			string[] tmp = m.Split(' ');
			user = tmp[0]; num = int.Parse(tmp[1]);
		}

		public override string ToString()
		{

			string tmp = "GETTRAIN " + user + " " + num;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (MPManager.IsServer())
			{
				foreach (Train t in Program.Simulator.Trains)
				{
					if (t == null) continue;
					if (t.Number == num) //found it, broadcast to everyone
					{
						MPManager.BroadCast((new MSGUpdateTrain(user, t, t.Number)).ToString());
					}
				}
			}
		}

	}
	#endregion MSGGetTrain

	#region MSGUncouple

	public class MSGUncouple : Message
	{
		public string user, newTrainName, carID, firstCarIDOld, firstCarIDNew;
		public int TileX1, TileZ1, mDirection1;
		public float X1, Z1, Travelled1, Speed1;
		public int trainDirection;
		public int TileX2, TileZ2, mDirection2;
		public float X2, Z2, Travelled2, Speed2;
		public int train2Direction;
		public int newTrainNumber;
		public int oldTrainNumber;
		public int whichIsPlayer;
		string[] ids1;
		string[] ids2;
		int[] flipped1;
		int[] flipped2;

		TrainCar FindCar(List<TrainCar> list, string id)
		{
			foreach (TrainCar car in list) if (car.CarID == id) return car;
			return null;
		}
		public MSGUncouple(string m)
		{
			string[] areas = m.Split('\t');
			user = areas[0].Trim();

			whichIsPlayer = int.Parse(areas[1].Trim());

			firstCarIDOld = areas[2].Trim();

			firstCarIDNew = areas[3].Trim();

			string[] tmp = areas[4].Split(' ');
			TileX1 = int.Parse(tmp[0]); TileZ1 = int.Parse(tmp[1]);
			X1 = float.Parse(tmp[2]); Z1 = float.Parse(tmp[3]); Travelled1 = float.Parse(tmp[4]); Speed1 = float.Parse(tmp[5]); trainDirection = int.Parse(tmp[6]);
			oldTrainNumber = int.Parse(tmp[7]);
			mDirection1 = int.Parse(tmp[8]);
			tmp = areas[5].Split('\n');
			ids1 = new string[tmp.Length - 1];
			flipped1 = new int[tmp.Length - 1];
			for (var i = 0; i < ids1.Length; i++)
			{
				string[] field = tmp[i].Split('\r');
				ids1[i] = field[0].Trim();
				flipped1[i] = int.Parse(field[1].Trim());
			}

			tmp = areas[6].Split(' ');
			TileX2 = int.Parse(tmp[0]); TileZ2 = int.Parse(tmp[1]);
			X2 = float.Parse(tmp[2]); Z2 = float.Parse(tmp[3]); Travelled2 = float.Parse(tmp[4]); Speed2 = float.Parse(tmp[5]); train2Direction = int.Parse(tmp[6]);
			newTrainNumber = int.Parse(tmp[7]);
			mDirection2 = int.Parse(tmp[8]);

			tmp = areas[7].Split('\n');
			ids2 = new string[tmp.Length - 1];
			flipped2 = new int[tmp.Length - 1];
			for (var i = 0; i < ids2.Length; i++)
			{
				string[] field = tmp[i].Split('\r');
				ids2[i] = field[0].Trim();
				flipped2[i] = int.Parse(field[1].Trim());
			}
		}

		public MSGUncouple(Train t, Train newT, string u, string ID, TrainCar car)
		{
			if (t.Cars.Count == 0 || newT.Cars.Count == 0) { user = ""; return; }//no cars in one of the train, not sure how to handle, so just return;
			TrainCar oldLead = t.LeadLocomotive;
			Train temp = null; int tmpNum;
			if (!t.Cars.Contains(Program.Simulator.PlayerLocomotive))
			{//the old train should have the player, otherwise, 
				tmpNum = t.Number; t.Number = newT.Number; newT.Number = tmpNum;
				temp = t; t = newT; newT = temp;
			}
			carID = ID;
			user = u;
			TileX1 = t.RearTDBTraveller.TileX; TileZ1 = t.RearTDBTraveller.TileZ; X1 = t.RearTDBTraveller.X; Z1 = t.RearTDBTraveller.Z; Travelled1 = t.travelled; Speed1 = t.SpeedMpS;
			trainDirection = t.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 0 : 1;//0 forward, 1 backward
			mDirection1 = (int)t.MUDirection;
			TileX2 = newT.RearTDBTraveller.TileX; TileZ2 = newT.RearTDBTraveller.TileZ; X2 = newT.RearTDBTraveller.X; Z2 = newT.RearTDBTraveller.Z; Travelled2 = newT.travelled; Speed2 = newT.SpeedMpS;
			train2Direction = newT.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 0 : 1;//0 forward, 1 backward
			mDirection2 = (int)newT.MUDirection;

			if (MPManager.IsServer()) newTrainNumber = newT.Number;//serer will use the correct number
			else
			{
				newTrainNumber = 1000000 + Program.Random.Next(1000000);//client: temporary assign a train number 1000000-2000000, will change to the correct one after receiving response from the server
				newT.TrainType = Train.TRAINTYPE.REMOTE; //by default, uncoupled train will be controlled by the server
			}
			if (!newT.Cars.Contains(Program.Simulator.PlayerLocomotive)) //if newT does not have player locomotive, it may be controlled remotely
			{
				foreach (TrainCar car1 in newT.Cars)
				{
					car1.Train = newT;
					foreach (var p in MPManager.OnlineTrains.Players)
					{
						if (car1.CarID.StartsWith(p.Value.LeadingLocomotiveID))
						{
							p.Value.Train = car1.Train;
							car1.Train.TrainType = Train.TRAINTYPE.REMOTE;
							break;
						}
					}
				}
			}

			if (!t.Cars.Contains(Program.Simulator.PlayerLocomotive)) //if t (old train) does not have player locomotive, it may be controlled remotely
			{
				foreach (TrainCar car1 in t.Cars)
				{
					car1.Train = t;
					foreach (var p in MPManager.OnlineTrains.Players)
					{
						if (car1.CarID.StartsWith(p.Value.LeadingLocomotiveID))
						{
							p.Value.Train = car1.Train;
							car1.Train.TrainType = Train.TRAINTYPE.REMOTE;
							break;
						}
					}
				}
			}


			if (t.Cars.Contains(Program.Simulator.PlayerLocomotive) || newT.Cars.Contains(Program.Simulator.PlayerLocomotive))
			{
				string info = "Trains uncoupled, gain back control by Shift-E";
				if (Program.Simulator.Confirmer != null)
					Program.Simulator.Confirmer.Information(info);
			}

			/*
			//if one of the train holds other player's lead locomotives
			foreach (var pair in MPManager.OnlineTrains.Players)
			{
				string check = pair.Key + " - 0";
				foreach (var car1 in t.Cars) if (car1.CarID.StartsWith(check)) { t.TrainType = Train.TRAINTYPE.REMOTE; break; }
				foreach (var car1 in newT.Cars) if (car1.CarID.StartsWith(check)) { newT.TrainType = Train.TRAINTYPE.REMOTE; break; }
			}*/
			oldTrainNumber = t.Number;
			newTrainName = "UC" + newTrainNumber; newT.Number = newTrainNumber;

			if (newT.LeadLocomotive != null) firstCarIDNew = "Leading " + newT.LeadLocomotive.CarID;
			else firstCarIDNew = "First " + newT.Cars[0].CarID;

			if (t.LeadLocomotive != null) firstCarIDOld = "Leading " + t.LeadLocomotive.CarID;
			else firstCarIDOld = "First " + t.Cars[0].CarID;

			ids1 = new string[t.Cars.Count];
			flipped1 = new int[t.Cars.Count];
			for (var i = 0; i < ids1.Length; i++)
			{
				ids1[i] = t.Cars[i].CarID;
				flipped1[i] = t.Cars[i].Flipped == true ? 1 : 0;
			}

			ids2 = new string[newT.Cars.Count];
			flipped2 = new int[newT.Cars.Count];
			for (var i = 0; i < ids2.Length; i++)
			{
				ids2[i] = newT.Cars[i].CarID;
				flipped2[i] = newT.Cars[i].Flipped == true ? 1 : 0;
			}

			//to see which train contains the car (PlayerLocomotive)
			if (t.Cars.Contains(car)) whichIsPlayer = 0;
			else if (newT.Cars.Contains(car)) whichIsPlayer = 1;
			else whichIsPlayer = 2;
		}

		string FillInString(int i)
		{
			string tmp = "";
			if (i == 1)
			{
				for (var j = 0; j < ids1.Length; j++)
				{
					tmp += ids1[j] + "\r" + flipped1[j] + "\n";
				}
			}
			else
			{
				for (var j = 0; j < ids2.Length; j++)
				{
					tmp += ids2[j] + "\r" + flipped2[j] + "\n";
				}
			}
			return tmp;
		}
		public override string ToString()
		{
			if (user == "") return "5: ALIVE"; //wrong, so just return an ALIVE string
			string tmp = "UNCOUPLE " + user + "\t" + whichIsPlayer + "\t" + firstCarIDOld + "\t" + firstCarIDNew
				+ "\t" + TileX1 + " " + TileZ1 + " " + X1 + " " + Z1 + " " + Travelled1 + " " + Speed1 + " " + trainDirection + " " + oldTrainNumber + " " + mDirection1 + "\t"
				+ FillInString(1)
				+ "\t" + TileX2 + " " + TileZ2 + " " + X2 + " " + Z2 + " " + Travelled2 + " " + Speed2 + " " + train2Direction + " " + newTrainNumber + " " + mDirection2 + "\t"
				+ FillInString(2);
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			bool oldIDIsLead = true, newIDIsLead = true;
			if (firstCarIDNew.StartsWith("First "))
			{
				firstCarIDNew = firstCarIDNew.Replace("First ", "");
				newIDIsLead = false;
			}
			else firstCarIDNew = firstCarIDNew.Replace("Leading ", "");
			if (firstCarIDOld.StartsWith("First "))
			{
				firstCarIDOld = firstCarIDOld.Replace("First ", "");
				oldIDIsLead = false;
			}
			else firstCarIDOld = firstCarIDOld.Replace("Leading ", "");

			if (user == MPManager.GetUserName()) //received from the server, but it is about mine action of uncouple
			{
				foreach (Train t in Program.Simulator.Trains)
				{
					foreach (TrainCar car in t.Cars)
					{
						if (car.CarID == firstCarIDOld)//got response about this train
						{
							t.Number = oldTrainNumber;
							if (oldIDIsLead == true) t.LeadLocomotive = car;
						}
						if (car.CarID == firstCarIDNew)//got response about this train
						{
							t.Number = newTrainNumber;
							if (newIDIsLead == true) t.LeadLocomotive = car;
						}
					}
				}

			}
			else
			{
				TrainCar lead = null;
				Train train = null;
				List<TrainCar> trainCars = null;
				foreach (Train t in Program.Simulator.Trains)
				{
					var found = false;
					foreach (TrainCar car in t.Cars)
					{
						if (car.CarID == firstCarIDOld)//got response about this train
						{
							found = true;
							break;
						}
					}
					if (found == true)
					{
						train = t;
						lead = train.LeadLocomotive;
						trainCars = t.Cars;
						List<TrainCar> tmpcars = new List<TrainCar>();
						for (var i = 0; i < ids1.Length; i++)
						{
							TrainCar car = FindCar(trainCars, ids1[i]);
							if (car == null) continue;
							car.Flipped = flipped1[i] == 0 ? false : true;
							tmpcars.Add(car); 
						}
						if (tmpcars.Count == 0) return;
						t.Cars = tmpcars;
						Traveller.TravellerDirection d1 = Traveller.TravellerDirection.Forward;
						if (trainDirection == 1) d1 = Traveller.TravellerDirection.Backward;
						t.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX1, TileZ1, X1, Z1, d1);
						t.CalculatePositionOfCars(0);  // fix the front traveller
						t.travelled = Travelled1;
						t.SpeedMpS = Speed1;
						t.LeadLocomotive = lead;
						t.MUDirection = (Direction)mDirection1;
						train.CheckFreight();
						//train may contain myself, and no other players, thus will make myself controlling this train
						if (train.Cars.Contains(Program.Simulator.PlayerLocomotive))
						{
							Program.Simulator.PlayerLocomotive.Train = train;
						}
						foreach (var c in train.Cars)
						{
							if (c.CarID == firstCarIDOld && oldIDIsLead) train.LeadLocomotive = c;
							foreach (var p in MPManager.OnlineTrains.Players)
							{
								if (p.Value.LeadingLocomotiveID == c.CarID) p.Value.Train = train;
							}
						}
						break;
					}
				}

				if (train == null || trainCars == null) return;

				Train train2 = new Train(Program.Simulator);
				List<TrainCar> tmpcars2 = new List<TrainCar>();
				for (var i = 0; i < ids2.Length; i++)
				{
					TrainCar car = FindCar(trainCars, ids2[i]);
					if (car == null) continue;
					tmpcars2.Add(car);
					car.Flipped = flipped2[i] == 0 ? false : true;
				}
				if (tmpcars2.Count == 0) return;
				train2.Cars = tmpcars2;
				train2.LeadLocomotive = null;
				train2.LeadNextLocomotive();
				train2.CheckFreight();

				//train2 may contain myself, and no other players, thus will make myself controlling this train
				/*if (train2.Cars.Contains(Program.Simulator.PlayerLocomotive))
				{
					var gainControl = true;
					foreach (var pair in MPManager.OnlineTrains.Players)
					{
						string check = pair.Key + " - 0";
						foreach (var car1 in train2.Cars) if (car1.CarID.StartsWith(check)) { gainControl = false; break; }
					}
					if (gainControl == true) { train2.TrainType = Train.TRAINTYPE.PLAYER; train2.LeadLocomotive = Program.Simulator.PlayerLocomotive; }
				}*/
				Traveller.TravellerDirection d2 = Traveller.TravellerDirection.Forward;
				if (train2Direction == 1) d2 = Traveller.TravellerDirection.Backward;

				// and fix up the travellers
				train2.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX2, TileZ2, X2, Z2, d2);
				train2.travelled = Travelled2;
				train2.SpeedMpS = Speed2;
				train2.MUDirection = (Direction)mDirection2;
				train2.CalculatePositionOfCars(0);  // fix the front traveller
				if (train.Cars.Contains(Program.Simulator.PlayerLocomotive))
				{
					Program.Simulator.PlayerLocomotive.Train = train;
				}
				foreach (TrainCar car in train2.Cars)
				{
					if (car.CarID == firstCarIDNew && newIDIsLead) train2.LeadLocomotive = car;
					car.Train = train2;
					foreach (var p in MPManager.OnlineTrains.Players)
					{
						if (car.CarID.StartsWith(p.Value.LeadingLocomotiveID))
						{
							p.Value.Train = car.Train;
							//car.Train.TrainType = Train.TRAINTYPE.REMOTE;
							break;
						}
					}
				}

				if (MPManager.IsServer()&& MPManager.Instance().AllowedManualSwitch) train2.InitializeSignals(false);
				train.UncoupledFrom = train2;
				train2.UncoupledFrom = train;

				if (train.Cars.Contains(Program.Simulator.PlayerLocomotive) || train2.Cars.Contains(Program.Simulator.PlayerLocomotive))
				{
					string info = "Trains uncoupled, gain back control by Shift-E";
					if (Program.Simulator.Confirmer != null)
						Program.Simulator.Confirmer.Information(info);
				}

				//if (whichIsPlayer == 0 && MPManager.OnlineTrains.findTrain(user) != null) MPManager.OnlineTrains.Players[user].Train = train;
				//else if (whichIsPlayer == 1 && MPManager.OnlineTrains.findTrain(user) != null) MPManager.OnlineTrains.Players[user].Train = train2; //the player may need to update the train it drives
				MPManager.Instance().AddOrRemoveTrain(train2, true);

				if (MPManager.IsServer())
				{
					this.newTrainNumber = train2.Number;//we got a new train number, will tell others.
					this.oldTrainNumber = train.Number;
					train2.LastReportedSpeed = 1;
					MPManager.BroadCast(this.ToString());//if server receives this, will tell others, including whoever sent the information
				}
				else
				{
					train2.TrainType = Train.TRAINTYPE.REMOTE;
					train2.Number = this.newTrainNumber; //client receives a message, will use the train number specified by the server
					train.Number = this.oldTrainNumber;
				}
			}
		}
	}
	#endregion MSGUncouple
	
	#region MSGCouple
	public class MSGCouple : Message
	{
		string[] cars;
		string[] ids;
		int[] flipped; //if a wagon is engine
		int TrainNum;
		int RemovedTrainNum;
		int direction;
		int TileX, TileZ, Lead, mDirection;
		float X, Z, Travelled;
		string whoControls;

		public MSGCouple(string m)
		{
			//System.Console.WriteLine(m);
			int index = m.IndexOf(' '); int last = 0;
			TrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			RemovedTrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			direction = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileX = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileZ = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			X = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Z = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Travelled = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Lead = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			whoControls = m.Substring(0, index + 1).Trim();
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			mDirection = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			string[] areas = m.Split('\t');
			cars = new string[areas.Length - 1];//with an empty "" at end
			ids = new string[areas.Length - 1];
			flipped = new int[areas.Length - 1];
			for (var i = 0; i < cars.Length; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
			}

			//System.Console.WriteLine(this.ToString());

		}

		public MSGCouple(Train t, Train oldT)
		{
			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].RealWagFilePath;
				ids[i] = t.Cars[i].CarID;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}
			TrainNum = t.Number;
			RemovedTrainNum = oldT.Number;
			direction = t.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 0 : 1;
			TileX = t.RearTDBTraveller.TileX;
			TileZ = t.RearTDBTraveller.TileZ;
			X = t.RearTDBTraveller.X;
			Z = t.RearTDBTraveller.Z;
			Travelled = t.travelled;
			MPManager.Instance().RemoveUncoupledTrains(t); //remove the trains from uncoupled train lists
			MPManager.Instance().RemoveUncoupledTrains(oldT);
			var j = 0;
			Lead = -1;
			foreach(TrainCar car in t.Cars) {
				if (car == t.LeadLocomotive) {Lead = j; break;}
				j++;
			}
			whoControls = "NA";
			var index = 0;
			if (t.LeadLocomotive != null) index = t.LeadLocomotive.CarID.IndexOf(" - 0");
			if (index > 0)
			{
				whoControls = t.LeadLocomotive.CarID.Substring(0, index);
			}
			foreach (var p in MPManager.OnlineTrains.Players)
			{
				if (p.Value.Train == oldT) { p.Value.Train = t; break; }
			}
			mDirection = (int)t.MUDirection;
			if (t.Cars.Contains(Program.Simulator.PlayerLocomotive))
			{
				string info = "Trains coupled, hit \\ then Shift-? to release brakes";
				if (Program.Simulator.Confirmer != null)
					Program.Simulator.Confirmer.Information(info);
			}
			MPManager.Instance().AddOrRemoveTrain(oldT, false); //remove the old train
		}

		public override string ToString()
		{
			string tmp = "COUPLE " + TrainNum + " " + RemovedTrainNum + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " " +Lead + " " + whoControls + " " + mDirection + " ";
			for (var i = 0; i < cars.Length; i++)
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\t";
			}
			return "" + tmp.Length + ": " + tmp;
		}

		private TrainCar FindCar(Train t1, Train t2, string carID)
		{
			foreach (TrainCar c in t1.Cars) if (c.CarID == carID) return c;
			foreach (TrainCar c in t2.Cars) if (c.CarID == carID) return c;
			return null;
		}
		public override void HandleMsg()
		{
			if (MPManager.IsServer()) return;//server will not receive this from client
			string PlayerTrainID;
			if (Program.Simulator.PlayerLocomotive != null) PlayerTrainID = Program.Simulator.PlayerLocomotive.CarID;
			else PlayerTrainID = "NULL";
			Train train = null, train2 = null;

			foreach (Train t in Program.Simulator.Trains)
			{
				if (t.Number == this.TrainNum) train = t;
				if (t.Number == this.RemovedTrainNum) train2 = t;
			}

			TrainCar lead = train.LeadLocomotive;
			if (lead == null) lead = train2.LeadLocomotive;

			/*if (Program.Simulator.PlayerLocomotive != null && Program.Simulator.PlayerLocomotive.Train == train2)
			{
				Train tmp = train2; train2 = train; train = tmp; Program.Simulator.PlayerLocomotive.Train = train;
			}*/

			if (train == null || train2 == null) return; //did not find the trains to op on

			//if (consistDirection != 1)
			//	train.RearTDBTraveller.ReverseDirection();
			TrainCar previousCar = null;
			List<TrainCar> tmpCars = new List<TrainCar>();
			for (var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				TrainCar car = FindCar(train, train2, ids[i]);
				if (car == null) continue;
				//car.PreviousCar = previousCar;
				bool flip = true;
				if (flipped[i] == 0) flip = false;
				car.Flipped = flip;
				car.CarID = ids[i];
				tmpCars.Add(car);
				car.Train = train;
				previousCar = car;

			}// for each rail car
			if (tmpCars.Count == 0) return;
			//List<TrainCar> oldList = train.Cars;
			train.Cars = tmpCars;
			
			train.travelled = Travelled;
			train.MUDirection = (Direction)mDirection;
			train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 0 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			train.CheckFreight();
			train.CalculatePositionOfCars(0);
			train.LeadLocomotive = null; train2.LeadLocomotive = null;
			if (Lead != -1 && Lead < train.Cars.Count ) train.LeadLocomotive = train.Cars[Lead];

			if (train.LeadLocomotive == null) train.LeadNextLocomotive();

			//mine is not the leading locomotive, thus I give up the control
			if (train.LeadLocomotive != Program.Simulator.PlayerLocomotive)
			{
				train.TrainType = Train.TRAINTYPE.REMOTE; //make the train remote controlled
			}

			if (MPManager.Instance().FindPlayerTrain(train2))
			{
				int count = 0;
				while (count < 3)
				{
					try
					{
						foreach (var p in MPManager.OnlineTrains.Players)
						{
							if (p.Value.Train == train2) p.Value.Train = train;
						}
						break;
					}
					catch (Exception) { count++; }
				}
			}

			//update the remote user's train
			if (MPManager.Instance().FindPlayerTrain(whoControls) != null) MPManager.OnlineTrains.Players[whoControls].Train = train;
			if (train.Cars.Contains(Program.Simulator.PlayerLocomotive)) Program.Simulator.PlayerLocomotive.Train = train;

			MPManager.Instance().AddOrRemoveTrain(train2, false);


			if (train.Cars.Contains(Program.Simulator.PlayerLocomotive))
			{
				string info = "Trains coupled, hit \\ then Shift-? to release brakes";
				if (Program.Simulator.Confirmer != null)
					Program.Simulator.Confirmer.Information(info);
			}
		}
	}
	#endregion MSGCouple

	#region MSGSignalStatus
	public class MSGSignalStatus : Message
	{
		static byte[] preState;
		static SortedList<int, SignalHead> signals;
		public bool OKtoSend = false;
		static byte[] signalsStates;
		int readed;
		//constructor to create a message from signal data
		public MSGSignalStatus()
		{
			var i = 0;
			if (signals == null)
			{
				signals = new SortedList<int, SignalHead>();
				if (Program.Simulator.Signals.SignalObjects != null)
				{
					foreach (var s in Program.Simulator.Signals.SignalObjects)
					{
						if (s != null && s.isSignal && s.SignalHeads != null)
							foreach (var h in s.SignalHeads)
							{
								//System.Console.WriteLine(h.TDBIndex);
								signals.Add(h.TDBIndex * 1000 + h.trItemIndex, h);
							}
					}
				}
				signalsStates = new byte[signals.Count+2];
			}
			if (preState == null)
			{
				preState = new byte[signals.Count + 2];
				for (i = 0; i < preState.Length; i++) preState[i] = 0;
			}

			i = 0;
			foreach (var t in signals)
			{
				signalsStates[i] = (byte)(t.Value.state + 1);
				//signalsStates[2 * i + 1] = (byte)(t.Value.draw_state + 1);
				i++;
				//msgx += (char)(((int)t.Value.state + 1) * 100 + (t.Value.draw_state + 1));
				//msgx += "" + (char)(t.Value.state + 1) + "" + (char)(t.Value.draw_state + 1);//avoid \0
			}
			OKtoSend = false;
			for (i = 0; i < signals.Count; i++)
			{
				if (signalsStates[i] != preState[i]) { OKtoSend = true; }//something is different, will send
				preState[i] = signalsStates[i];
			}
			if (OKtoSend == false)
			{
				//new player added, will keep sending for a while
				if (Program.Simulator.GameTime - MPManager.Instance().lastPlayerAddedTime < 3 * MPManager.Instance().MPUpdateInterval) OKtoSend = true;
			}
		}

		//constructor to decode the message "m"
		public MSGSignalStatus(string m)
		{
			if (signals == null)
			{
				signals = new SortedList<int, SignalHead>();
				try
				{
					if (Program.Simulator.Signals.SignalObjects != null)
					{
						foreach (var s in Program.Simulator.Signals.SignalObjects)
						{
							if (s != null && s.isSignal && s.SignalHeads != null)
								foreach (var h in s.SignalHeads)
								{
									//System.Console.WriteLine(h.TDBIndex);
									signals.Add(h.TDBIndex * 1000 + h.trItemIndex, h);
								}
						}
					}
					signalsStates = new byte[signals.Count+128];
				}
				catch (Exception e) { signals = null; throw e; }//error, clean the list, so we can get another signal
			}
			byte[] gZipBuffer = Convert.FromBase64String(m);
			using (var memoryStream = new MemoryStream())
			{
				int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
				memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

				memoryStream.Position = 0;
				using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
				{
					readed = gZipStream.Read(signalsStates, 0, signalsStates.Length);
				}
			}
		}

		//how to handle the message?
		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (Program.Server != null) return; //server will ignore it

			//if (signals.Count != readed/2-2) { System.Console.WriteLine("Error in synchronizing signals " + signals.Count + " " + readed); return; }
			int i = 0;
			foreach (var t in signals)
			{
				t.Value.state = (SignalHead.SIGASP)(signalsStates[1 * i] - 1); //we added 1 when build the message, need to subtract it out
				//t.Value.draw_state = (int)(signalsStates[2 * i + 1] - 1);
				t.Value.draw_state = t.Value.def_draw_state(t.Value.state);
				//System.Console.Write(msgx[i]-48);
				i++;
			}
			//System.Console.Write("\n");

		}

		public override string ToString()
		{
			byte[] buffer = signalsStates;
			var memoryStream = new MemoryStream();
			using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
			{
				gZipStream.Write(buffer, 0, buffer.Length);
			}

			memoryStream.Position = 0;

			var compressedData = new byte[memoryStream.Length];
			memoryStream.Read(compressedData, 0, compressedData.Length);

			var gZipBuffer = new byte[compressedData.Length + 4];
			Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
			string tmp = "SIGNALSTATES " + Convert.ToBase64String(gZipBuffer); // fill in the message body here
			return "" + tmp.Length + ": " + tmp;
		}
	}
	#endregion MSGSignalStatus

	#region MSGLocoInfo
	public class MSGLocoInfo : Message
	{

		float EB, DB, TT, VL, CC, BC, DC, FC, I1, I2;
		string user;
		int tnum; //train number

		//constructor to create a message from signal data
		public MSGLocoInfo(TrainCar c, string u)
		{
			MSTSLocomotive loco = (MSTSLocomotive)c;
			EB = DB = TT = VL = CC = BC = DC = FC = I1 = I2 = 0.0f;
			if (loco is MSTSSteamLocomotive)
			{
				MSTSSteamLocomotive loco1 = (MSTSSteamLocomotive)loco;
				loco1.GetLocoInfo(ref CC, ref BC, ref DC, ref FC, ref I1, ref I2);
			}
			if (loco.EngineBrakeController != null)
			{
				EB = loco.EngineBrakeController.CurrentValue;
			}
			if (loco.DynamicBrakeController != null)
			{
				DB = loco.DynamicBrakeController.CurrentValue;
			}
			TT = loco.ThrottleController.CurrentValue;
			if (loco is MSTSElectricLocomotive)
			{
				VL = (loco as MSTSElectricLocomotive).VoltageV;
			}
			tnum = loco.Train.Number;
			user = u;
		}

		//constructor to decode the message "m"
		public MSGLocoInfo(string m)
		{
			string[] tmp = m.Split('\t');
			user = tmp[0].Trim();
			tnum = int.Parse(tmp[1]);
			EB = float.Parse(tmp[2]);
			DB = float.Parse(tmp[3]);
			TT = float.Parse(tmp[4]);
			VL = float.Parse(tmp[5]);
			CC = float.Parse(tmp[6]);
			BC = float.Parse(tmp[7]);
			DC = float.Parse(tmp[8]);
			FC = float.Parse(tmp[9]);
			I1 = float.Parse(tmp[10]);
			I2 = float.Parse(tmp[11]);
		}

		//how to handle the message?
		public override void HandleMsg() //only client will get message, thus will set states
		{
			foreach (Train t in Program.Simulator.Trains)
			{
				if (t.TrainType != Train.TRAINTYPE.REMOTE && t.Number == tnum)
				{
					foreach (var car in t.Cars)
					{
						if (car.CarID.StartsWith(user) && car is MSTSLocomotive)
						{
							updateValue((MSTSLocomotive)car);
						}
					}
					return;
				}
			}
		}

		private void updateValue(MSTSLocomotive loco)
		{
			if (loco is MSTSSteamLocomotive)
			{
				MSTSSteamLocomotive loco1 = (MSTSSteamLocomotive)loco;
				loco1.GetLocoInfo(ref CC, ref BC, ref DC, ref FC, ref I1, ref I2);
			}
			if (loco.EngineBrakeController != null)
			{
				loco.EngineBrakeController.CurrentValue = EB;
				loco.EngineBrakeController.UpdateValue = 0.0f;
			}
			if (loco.DynamicBrakeController != null)
			{
				loco.DynamicBrakeController.CurrentValue = DB;
				loco.DynamicBrakeController.UpdateValue = 0.0f;
			}
			loco.ThrottleController.CurrentValue = TT;
			loco.ThrottleController.UpdateValue = 0.0f;
			if (loco is MSTSElectricLocomotive)
			{
				(loco as MSTSElectricLocomotive).VoltageV = VL;
			}
			loco.notificationReceived = true;
		}
		public override string ToString()
		{
			string tmp = "LOCOINFO " + user + "\t" + tnum + "\t" + EB + "\t" + DB + "\t" + TT + "\t" + VL + "\t" + CC + "\t" + BC + "\t" + DC + "\t" + FC + "\t" + I1 + "\t" + I2; // fill in the message body here
			return "" + tmp.Length + ": " + tmp;
		}
	}
	#endregion MSGLocoInfo

	#region MSGAvatar
	public class MSGAvatar : Message
	{
		public string user;
		public string url;
		public MSGAvatar(string m)
		{
			var tmp = m.Split('\t');
			user = tmp[0].Trim();
			url = tmp[1];
		}

		public MSGAvatar(string u, string l)
		{
			user = u;
			url = l;
		}

		public override string ToString()
		{

			string tmp = "AVATAR " + user +"\n" + url;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) return; //avoid myself

			foreach (var p in MPManager.OnlineTrains.Players)
			{
				if (p.Key == user) p.Value.url = url;
				Program.DebugViewer.AddAvatar(user, url);
			}

			if (MPManager.IsServer())
			{
				MPManager.BroadCast((new MSGAvatar(user, url)).ToString());
			}
		}

	}

	#endregion MSGAvatar


	#region MSGText
	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGText : MSGRequired
	{
		string msgx;
		string sender;
		string user;
		public MSGText(string m)
		{
			string[] t = m.Split('\t');
			sender = t[0].Trim();
			user = t[1].Trim();
			msgx = t[2];

		}

		public MSGText(string s, string u, string m)
		{
			sender = s.Trim();
			user = u;
			msgx = m;
		}

		public override void HandleMsg()
		{
			if (sender == MPManager.GetUserName()) return; //avoid my own msg
			string[] users = user.Split('\r');
			foreach (var name in users)
			{
				//someone may send a message with 0Server, which is intended for the server
				if (name.Trim() == MPManager.GetUserName() || (MPManager.IsServer()&&name.Trim()=="0Server"))
				{
					System.Console.WriteLine("MSG from " + sender + ":" + msgx);
					MPManager.Instance().lastSender = sender;
					if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.MSG(" From "+ sender+": "+msgx);
					Program.DebugViewer.addNewMessage(Program.Simulator.GameTime, sender + ": " + msgx);
					break;
				}
			}
			if (MPManager.IsServer())//server check if need to tell others.
			{
				//System.Console.WriteLine(users);
				if (users.Count() == 1 && users[0].Trim() == MPManager.GetUserName()) return;
				if (users.Count() == 1 && users[0].Trim() == "0Server") return;
				//System.Console.WriteLine(this.ToString());
				MultiPlayer.MPManager.BroadCast(this.ToString());
			}
		}

		public override string ToString()
		{
			string tmp = "TEXT " + sender + "\t" + user + "\t" + msgx;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	#endregion MSGText

	#region MSGWeather
	public class MSGWeather : Message
	{
		public  int weather;
		public float overcast;
		public MSGWeather(string m)
		{
			var tmp = m.Split(' ');
			weather = int.Parse(tmp[0]);
			overcast = float.Parse(tmp[1]);
		}

		public MSGWeather(int w, float o)
		{
			weather = -1; overcast = -1f;
			if (w >= 0) weather = w;
			if (o > 0) overcast = o;
		}

		public override string ToString()
		{

			string tmp = "WEATHER " + weather + " " + overcast;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (MPManager.IsServer()) return;
			if (weather >= 0)
			{
				MPManager.Instance().newWeather = weather;
				MPManager.Instance().overCast = -1;
			}
			if (overcast >= 0)
			{
				MPManager.Instance().newWeather = -1;
				MPManager.Instance().overCast = overcast;
			}
			MPManager.Instance().weatherChanged = true;
		}

	}

	#endregion MSGWeather

	#region MSGAider
	public class MSGAider : Message
	{
		public string user;
		public bool add;
		public MSGAider(string m)
		{
			string[] tmp = m.Split('\t');
			user = tmp[0].Trim();
			if (tmp[1].Trim() == "T") add = true; else add = false;
		}

		public MSGAider(string m, bool add1)
		{
			user = m.Trim();
			add = add1;
		}

		public override string ToString()
		{

			string tmp = "AIDER " + user + "\t" + (add == true ? "T" : "F");
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (MPManager.IsServer()) return;
			if (add == true)
			{
				MPManager.Instance().AmAider = true;
			}
			else
			{
				MPManager.Instance().AmAider = false;
			}
		}

	}

	#endregion MSGAider

}
