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
			if ((this.OSFamily != PlatformID.Other) && (this.OSVersion.Major > 0)) {
				return this.OSFamily + " " + this.OSVersion;
			} else {
				return orElse;
			}
		}

		public override readonly string ToString()
		{
			return this.VolumeName;
		}
	}
}
