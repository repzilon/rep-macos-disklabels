//
//  Program.cs
//
//  Author:
//       René Rhéaume <repzilon@users.noreply.github.com>
//
//  Copyright (c) 2024 René Rhéaume
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Repzilon.Utilities.MacOSX.DiskLabels
{
	internal enum PropertyListReading : byte
	{
		NotFound,
		KeyWasRead,
		ValueWasRead
	}

	internal static class Program
	{
		private static Process StartExec(string fullExecutablePath, string commandLineArguments)
		{
			var psi = new ProcessStartInfo();
			psi.UseShellExecute = false;
			psi.FileName = fullExecutablePath;
			psi.Arguments = commandLineArguments;
			psi.RedirectStandardOutput = true;
			psi.CreateNoWindow = false;
			var process = new Process();
			process.StartInfo = psi;
			process.Start();
			return process;
		}

		static void Main(string[] args)
		{
			VolumeInfo vol;

#if false
			var drvarDotNet = DriveInfo.GetDrives();
			var lstDriveInfoVolumes = new List<VolumeInfo>(drvarDotNet.Length);
			for (var i = 0; i < drvarDotNet.Length; i++) {
				vol = new VolumeInfo();
				vol.Capacity = drvarDotNet[i].TotalSize;
				vol.FileSystemFormat = drvarDotNet[i].DriveFormat;
				vol.MountPoint = drvarDotNet[i].Name;
				vol.OSFamily = PlatformID.Other;
				if (vol.MountPoint.StartsWith("/Volumes/")) {
					vol.VolumeName = vol.MountPoint.Replace("/Volumes/", "");
				}
				vol.Mounted = drvarDotNet[i].IsReady;
				lstDriveInfoVolumes.Add(vol);
			}
			lstDriveInfoVolumes.Sort(delegate (VolumeInfo a, VolumeInfo b) {
				return String.Compare(a.MountPoint, b.MountPoint, true);
			});
#endif

			var lstDiskutilVolumes = new List<VolumeInfo>();
			var prcDiskUtilList = StartExec("/usr/sbin/diskutil", "list");

			while (!prcDiskUtilList.StandardOutput.EndOfStream) {
				var strLine = prcDiskUtilList.StandardOutput.ReadLine();
				if (!String.IsNullOrEmpty(strLine)) {
					var mtcDiskutil = Regex.Match(strLine, @"[0-9]+:[ ]+(Apple_APFS|Microsoft Basic Data|APFS Volume|EFI|Apple_HFS)[ ](.+)[ ]([0-9.]+) ([KMGT])B[ ]+(disk[0-9]+s[0-9])");
					if ((mtcDiskutil != null) && mtcDiskutil.Success) {
						var grcDiskutil = mtcDiskutil.Groups;
						vol = new VolumeInfo();
						vol.Capacity = CapacityFromDiskutil(grcDiskutil[3].Value, grcDiskutil[4].Value[0]);
						vol.DeviceNode = grcDiskutil[5].Value;
						vol.FileSystemFormat = FileSystemFormatFromDiskutil(grcDiskutil[1].Value);
						vol.VolumeName = grcDiskutil[2].Value.Trim();
						vol.OSFamily = PlatformID.Other;
						lstDiskutilVolumes.Add(vol);
					}
				}
			}

			var strarInVolumes = Directory.GetDirectories("/Volumes");
			int c1 = lstDiskutilVolumes.Count;
			for (int i = 0; i < c1; i++) {
				vol = lstDiskutilVolumes[i];

				var prcDiskUtilInfo = StartExec("/usr/sbin/diskutil", "info " + vol.DeviceNode);
				while (!prcDiskUtilInfo.StandardOutput.EndOfStream) {
					var strLine = prcDiskUtilInfo.StandardOutput.ReadLine();
					if (!String.IsNullOrEmpty(strLine)) {
						if (strLine.Contains("Volume Name:")) {
							if (!vol.VolumeName.StartsWith("Container disk")) {
								vol.VolumeName = strLine.Replace("Volume Name:", "").Trim();
							}
						} else if (strLine.Contains("Mounted:")) {
							vol.Mounted = strLine.Contains("Yes");
						} else if (strLine.Contains("Mount Point:")) {
							vol.MountPoint = strLine.Replace("Mount Point:", "").Trim();
						} else if (strLine.Contains("File System Personality:")) {
							if (vol.FileSystemFormat != "efi") {
								vol.FileSystemFormat = FileSystemFormatFromDiskutil(strLine.Replace("File System Personality:", "").Trim());
							}
						} else if (strLine.Contains("Disk / Partition UUID:")) {
							vol.Identifier = Guid.Parse(strLine.Replace("Disk / Partition UUID:", "").Trim());
						} else if (strLine.Contains("Container Total Space:") || strLine.Contains("Volume Total Space:")) {
							var strData = strLine.Replace("Container Total Space:", "").Replace("Volume Total Space:", "").Trim();
							var mtcCapacity = Regex.Match(strData, @"[0-9.]+ [A-Z]B [(]([0-9]+) Bytes");
							if ((mtcCapacity != null) && mtcCapacity.Success) {
								vol.Capacity = Int64.Parse(mtcCapacity.Groups[1].Value);
							}
						}
					}
				}

				if (vol.MountPoint == null) {
					var strStdMountPoint = Path.Combine("/Volumes", vol.VolumeName);
					if (Directory.Exists(strStdMountPoint)) {
						vol.MountPoint = strStdMountPoint;
					}
				}

				if (vol.MountPoint != null) {
					var driMount = new DirectoryInfo(vol.MountPoint);
					vol.MountPoint = (driMount.LinkTarget != null) ? driMount.LinkTarget : vol.MountPoint;
					var strMacosVersionPlistPath = Path.Combine(vol.MountPoint, "System/Library/CoreServices/SystemVersion.plist");
					if (File.Exists(strMacosVersionPlistPath)) {
						vol.Mounted = true;
						vol.OSFamily = PlatformID.MacOSX;

						using (var smrVersionPlist = File.OpenText(strMacosVersionPlistPath)) {
							var enuProductVersion = PropertyListReading.NotFound;
							while (!smrVersionPlist.EndOfStream && (enuProductVersion != PropertyListReading.ValueWasRead)) {
								var strLine = smrVersionPlist.ReadLine();
								if (!String.IsNullOrEmpty(strLine)) {
									if (enuProductVersion == PropertyListReading.KeyWasRead) {
										vol.OSVersion = Version.Parse(strLine.Trim().Replace("</string>", "").Replace("<string>", "").Trim());
										enuProductVersion = PropertyListReading.ValueWasRead;
									} else if (strLine.Contains("ProductVersion")) {
										enuProductVersion = PropertyListReading.KeyWasRead;
									}
								}
							}
						}
					}
				}

				var strBootLabelFile = Path.Combine("/System/Volumes/Preboot", vol.Identifier.ToString().ToUpperInvariant(), "System/Library/CoreServices/.disk_label.contentDetails");
				if (File.Exists(strBootLabelFile)) {
					vol.OpenCoreLabel = File.ReadAllText(strBootLabelFile);
				}

				lstDiskutilVolumes[i] = vol;
			}

			Console.WriteLine("From diskutil executable");
			OutputVolumes(lstDiskutilVolumes);
#if false
			Console.Write(Environment.NewLine);
			Console.WriteLine("From System.DriveInfo");
			OutputVolumes(lstDriveInfoVolumes);
#endif

			// echo "$label" > /System/Volumes/Preboot/$uuid/System/Library/CoreServices/.disk_label.contentDetails 
		}

		private static long CapacityFromDiskutil(string number, char prefix)
		{
			const int kThousand = 1000;
			double f64Capacity = Double.Parse(number, CultureInfo.InvariantCulture);
			if (prefix == 'T') {
				f64Capacity *= kThousand * kThousand * kThousand * (long)kThousand;
			} else if (prefix == 'G') {
				f64Capacity *= kThousand * kThousand * kThousand;
			} else if (prefix == 'M') {
				f64Capacity *= kThousand * kThousand;
			} else if (prefix == 'K') {
				f64Capacity *= kThousand;
			}
			return Convert.ToInt64(f64Capacity);
		}

		private static string FileSystemFormatFromDiskutil(string diskUtilType)
		{
			if (diskUtilType == "Apple_APFS") {
				return "container_apfs";
			} else if (diskUtilType == "APFS Volume") {
				return "apfs";
			} else if (diskUtilType == "Apple_HFS") {
				return "hfs";
			} else if ((diskUtilType == "Microsoft Basic Data") || (diskUtilType == "MS-DOS FAT32")) {
				return "vfat";
			} else if (diskUtilType == "Journaled HFS+") {
				return "hfs+j";
			} else {
				return diskUtilType.ToLowerInvariant();
			}
		}

		private static void OutputVolumes(IList<VolumeInfo> volumes)
		{
			Console.WriteLine("Device node|Filesysyem type|1024-blocks  |Ready|Installed OS  |Name");
			Console.WriteLine("\tUUID                                 |Boot Label|Mount point");
			Console.WriteLine(new String('-', 78));
			var c = volumes.Count;
			for (int i = 0; i < c; i++) {
				var v = volumes[i];
				Console.WriteLine("{0,-11}|{1,-15}|{2,13:n0}|  {3}  |{4,-14}|{5}",
				 v.DeviceNode, v.FileSystemFormat, v.Capacity / 1024, v.Mounted ? 'Y' : ' ', v.OperatingSystem(), v.VolumeName);
				Console.WriteLine("\t{0}|{1,-10}|{2}", v.Identifier.ToString().ToUpperInvariant(), v.OpenCoreLabel, v.MountPoint);
			}
		}
	}
}
