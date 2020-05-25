using AutoMapper;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using ShellSquare.Client.ETP.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP
{
    public static class ETPMapper
    {
        private static bool _isInitialized;
        private static Mapper mapper;
        public static Mapper Instance()
        {
            if (!_isInitialized)
            {
                AutoMapper.MapperConfiguration config = new AutoMapper.MapperConfiguration(c => {
                    c.CreateMap<ChannelMetadataRecord, ChannelMetaDataVM>()
                        .ForMember(dest => dest.StartIndex, opt => opt.MapFrom<StartIndexResolver>())
                        .ForMember(dest => dest.EndIndex, opt => opt.MapFrom<EndIndexResolver>());
                    c.CreateMap<IndexMetadataRecord, IndexMetadataRecordVM>();
                    c.CreateMap<DataObject, DataObjectVM>();
                    c.CreateMap<Resource, ResourceVM>();
                });
                mapper = new AutoMapper.Mapper(config);
                _isInitialized = true;
            }

            return mapper;
        }
    }
}
