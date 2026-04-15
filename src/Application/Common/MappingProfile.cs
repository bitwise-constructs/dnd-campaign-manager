using AutoMapper;
using DndCampaignManager.Application.Common.Models;
using DndCampaignManager.Domain.Entities;

namespace DndCampaignManager.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<MagicItem, MagicItemDto>();

        CreateMap<TreasureTable, TreasureTableDto>();

        CreateMap<TreasureTableEntry, TreasureTableEntryDto>()
            .ForMember(d => d.MagicItemName, opt => opt.MapFrom(s => s.MagicItem.Name))
            .ForMember(d => d.MagicItemRarity, opt => opt.MapFrom(s => s.MagicItem.Rarity))
            .ForMember(d => d.MagicItemCategory, opt => opt.MapFrom(s => s.MagicItem.Category));

        CreateMap<Campaign, CampaignDto>()
            .ForMember(d => d.CharacterCount, opt => opt.MapFrom(s => s.Characters.Count))
            .ForMember(d => d.MagicItemCount, opt => opt.MapFrom(s => s.MagicItems.Count));

        // Note: Character and WishlistItem use manual mapping for privacy filtering
        // and custom item support respectively. No AutoMapper profiles for those.
    }
}
