using AutoMapper;
using Energistics.Datatypes.ChannelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP.ViewModel
{
    public class StartIndexResolver : IValueResolver<ChannelMetadataRecord, ChannelMetaDataVM, IndexData_VM>
	{
		DateTime m_Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime();
		public IndexData_VM Resolve(ChannelMetadataRecord source, ChannelMetaDataVM destination, IndexData_VM member, ResolutionContext context)
		{
			member = new IndexData_VM();
			member.IndexValue = source.StartIndex.HasValue? source.StartIndex.Value : 0;
			double scale = source.Indexes.FirstOrDefault()?.Scale == null ? 0 : (double)source.Indexes.FirstOrDefault()?.Scale;
			if (source.Indexes.FirstOrDefault()?.IndexType == ChannelIndexTypes.Depth)
			{
				member.Depth = member.IndexValue / Math.Pow(10, scale);
			}
			else
			{
				var ticks = (member.IndexValue * 10) + m_Epoch.Ticks;
				member.Time = new DateTime(ticks);
			}

			return member;
		}
	}
}
