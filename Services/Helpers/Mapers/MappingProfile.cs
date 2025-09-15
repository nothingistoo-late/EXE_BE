using AutoMapper;
using DTOs;
using DTOs.BoxType.Respond;
using DTOs.Customer.Responds;
using DTOs.DiscountDTOs.Request;
using DTOs.DiscountDTOs.Respond;
using DTOs.OrderDTOs.Respond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Service mappings
            //CreateMap<Service, AddServiceRequestDTO>().ReverseMap();
            //CreateMap<Service, ServiceRespondDTO>().ReverseMap();
            //CreateMap<Membership, MembershipResponse>().ReverseMap();
            //CreateMap<CreateMembershipRequest, Membership>().ReverseMap();

            //// OrderDetail -> OrderDetailRespondDTO
            //CreateMap<OrderDetail, OrderDetailRespondDTO>()
            //        .ForMember(dest => dest.Rating,
            //                   opt => opt.MapFrom(src => src.Rating))
            //        .ForMember(dest => dest.Id,
            //                   opt => opt.MapFrom(src => src.OrderDetailId))
            //        .ForMember(dest => dest.ServiceName,
            //                   opt => opt.MapFrom(src => src.Service.Name))
            //        .ForMember(dest => dest.StaffName,
            //                   opt => opt.MapFrom(src => src.Staff.User.FirstName + " " + src.Staff.User.LastName))
            //        .ForMember(dest => dest.ScheduledTime,
            //                    opt => opt.MapFrom(src => src.ScheduleTime))
            //         .ForMember(dest => dest.Rating,
            //                    opt => opt.MapFrom(src => src.Rating)) // Rating → RatingDTO
            //        .ForMember(dest => dest.Note,
            //                    opt => opt.MapFrom(src => src.Note)); // optional, nhưng để rõ hơn cũng good; // 💥 FIX CHÍNH!

            //// 💥 ADD THIS: Order -> OrderRespondDTO
            //CreateMap<Order, OrderRespondDTO>();
            //CreateMap<Order, OrderResponse>();


            // Customer -> CreateCustomerRequestDTO
            CreateMap<CreateCustomerRequestDTO, User>()
                 .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                 .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                 .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                 ; 

            // Customer -> CustomerRespondDTO
            CreateMap<Customer, CustomerRespondDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.User.Gender.ToString()))
                .ReverseMap();

            //CreateMap<StaffSchedule, StaffScheduleRespondDTO>()
            //         .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\:mm")))
            //         .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\:mm")))
            //         .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
            //         .ForMember(dest => dest.staffName, opt => opt.MapFrom(src => src.Staff.User.FullName));


            //CreateMap<Staff, StaffRespondDTO>()
            //        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            //        .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            //        .ForMember(dest => dest.ImgURL, opt => opt.MapFrom(src => src.ImgURL))
            //        .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note));

           CreateMap<Customer, MyProfileResponse>()
          .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
          .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
          .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.PhoneNumber))
          .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
          .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.User.Gender))
          .ForMember(dest => dest.ImgURL, opt => opt.MapFrom(src => src.ImgURL))
          .ReverseMap();

            //CreateMap<CustomerMembership, CustomerMembershipResponse>()
            //.ForMember(dest => dest.MembershipName, opt => opt.MapFrom(src => src.Membership.Name))
            //.ForMember(dest => dest.DiscountPercentage, opt => opt.MapFrom(src => src.Membership.DiscountPercentage));
            //;
            //CreateMap<Rating, RatingDTO>().ReverseMap();

            // Map Order -> OrderResponse
            CreateMap<Order, OrderResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId)) // giả sử Order có CustomerId
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));

            // Map OrderDetail -> OrderDetailResponse
            CreateMap<OrderDetail, OrderDetailResponse>()
                .ForMember(dest => dest.BoxName, opt => opt.MapFrom(src => src.BoxType != null ? src.BoxType.Name : string.Empty));

            // Map từ CreateDTO -> Entity
            CreateMap<DiscountCreateDTO, Discount>().ReverseMap();

            // Map từ Entity -> RespondDTO
            CreateMap<Discount, DiscountRespondDTO>().ReverseMap();

            CreateMap<BoxTypes, BoxTypeRespondDTO>().ReverseMap();


        }
    }
}
