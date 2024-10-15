//
//  VolumeInfo.cs
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

namespace Repzilon.Utilities.MacOSX.DiskLabels
{
	internal struct VolumeInfo
	{
		public string DeviceNode;
		public string VolumeName;
		public string OpenCoreLabel;
		public string FileSystemFormat;
		public string MountPoint;
		public long Capacity;
		public Guid Identifier;
		public PlatformID OSFamily;
		public Version OSVersion;
		public Version KernelVersion;
		public bool Mounted;

		public string OperatingSystemOrFilesystemType()
		{
			return OperatingSystem(this.FileSystemFormat);
		}

		public string OperatingSystem()
		{
			return OperatingSystem("");
		}

		private string OperatingSystem(string orElse)
		{
			var family = this.OSFamily;
			var version = this.OSVersion;
			if ((family != PlatformID.Other) && (version.Major > 0)) {
				return family.ToString("g") + " " + version.ToString(3);
			} else {
				return orElse;
			}
		}

		public string IdentifierString()
		{
			return this.Identifier.ToString("D").ToUpperInvariant();
		}

		public override readonly string ToString()
		{
			return this.VolumeName;
		}
	}
}
