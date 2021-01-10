using AutoMapper;
using Moodful.Models;
using Moodful.Services.Storage.TableEntities;

namespace Moodful
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ReviewTableEntity, Review>()
                .ReverseMap()
                .ForMember(dest => dest.RowKey, opts => opts.MapFrom(src => src.Id));

            CreateMap<TagTableEntity, Tag>()
               .ReverseMap()
               .ForMember(dest => dest.RowKey, opts => opts.MapFrom(src => src.Id));
        }
    }
}
