using AutoMapper;
using DTOs;
using DTOs.BoxType.Respond;
using DTOs.CartDTOs.Respond;
using DTOs.Customer.Responds;
using DTOs.CustomerSubscriptionRequest.Respond;
using DTOs.DiscountDTOs.Request;
using DTOs.DiscountDTOs.Respond;
using DTOs.OrderDTOs.Respond;
using DTOs.AiMenuDTOs.Request;
using DTOs.AiMenuDTOs.Response;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;
using BusinessObjects;
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
                 .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src =>
                     string.IsNullOrWhiteSpace(src.FullName) ? string.Empty :
                     src.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty))
                 .ForMember(dest => dest.LastName, opt => opt.MapFrom(src =>
                     string.IsNullOrWhiteSpace(src.FullName) ? string.Empty :
                     string.Join(" ", src.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))))
                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));

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
          .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
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
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.FinalPrice))
                .ForMember(dest => dest.DiscountCode, opt => opt.MapFrom(src => src.DiscountCode))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.DeliveryTo, opt => opt.MapFrom(src => src.DeliveryTo))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.AllergyNote, opt => opt.MapFrom(src => src.AllergyNote))
                .ForMember(dest => dest.PreferenceNote, opt => opt.MapFrom(src => src.PreferenceNote))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.PayOSPaymentUrl, opt => opt.MapFrom(src => src.PayOSPaymentUrl))
                .ForMember(dest => dest.PayOSOrderCode, opt => opt.MapFrom(src => src.PayOSOrderCode))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));

            // Map OrderDetail -> OrderDetailResponse
            CreateMap<OrderDetail, OrderDetailResponse>()
                .ForMember(dest => dest.BoxName, opt => opt.MapFrom(src => src.BoxType != null ? src.BoxType.Name : string.Empty));

            // Map Order -> WeeklyOrderResponse (for weekly package orders)
            CreateMap<Order, WeeklyOrderResponse>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.FinalPrice))
                .ForMember(dest => dest.DiscountCode, opt => opt.MapFrom(src => src.DiscountCode))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.DeliveryTo, opt => opt.MapFrom(src => src.DeliveryTo))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));

            // Map từ CreateDTO -> Entity
            CreateMap<DiscountCreateDTO, Discount>().ReverseMap();

            // Map từ Entity -> RespondDTO
            CreateMap<Discount, DiscountRespondDTO>().ReverseMap();

            CreateMap<BoxTypes, BoxTypeRespondDTO>().ReverseMap();

            CreateMap<Order, CartResponse>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderDetails)).ReverseMap();

            // Map từ OrderDetail sang CartItemResponse
            CreateMap<OrderDetail, CartItemResponse>()
                .ForMember(dest => dest.BoxTypeName,opt => opt.MapFrom(src => src.BoxType != null ? src.BoxType.Name : null)).ReverseMap();
            // Map CustomerSubscription -> CustomerSubscriptionResponse
            CreateMap<CustomerSubscription, CustomerSubscriptionResponse>()
                .ForMember(dest => dest.HealthSurveyId,
                    opt => opt.MapFrom(src => src.HealthSurvey != null ? src.HealthSurvey.Id : Guid.Empty))
                .ForMember(dest => dest.Allergy,
                    opt => opt.MapFrom(src => src.HealthSurvey != null ? src.HealthSurvey.Allergy : null))
                .ForMember(dest => dest.Feeling,
                    opt => opt.MapFrom(src => src.HealthSurvey != null ? src.HealthSurvey.Feeling : null))
                // Nếu Status / PaymentStatus là enum -> convert sang string
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()));

            // AI Menu Mappings
            // AiRecipe -> AiRecipeResponse
            CreateMap<AiRecipe, AiRecipeResponse>()
                .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(src.Ingredients) ?? new List<string>()))
                .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(src.Instructions) ?? new List<string>()));

            // Notification Mappings
            // Notification -> UserNotificationResponse (simplified for users)
            CreateMap<Notification, UserNotificationResponse>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => 
                    (src.Sender.FirstName ?? "") + " " + (src.Sender.LastName ?? "")))
                .ForMember(dest => dest.IsBroadcast, opt => opt.MapFrom(src => src.ReceiverId == null))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

            // Notification -> AdminNotificationResponse (full details for admin)
            CreateMap<Notification, AdminNotificationResponse>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => 
                    (src.Sender.FirstName ?? "") + " " + (src.Sender.LastName ?? "")))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => 
                    src.Receiver != null ? (src.Receiver.FirstName ?? "") + " " + (src.Receiver.LastName ?? "") : null))
                .ForMember(dest => dest.IsBroadcast, opt => opt.MapFrom(src => src.ReceiverId == null))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

            // Notification -> SendToUserNotificationResponse
            CreateMap<Notification, SendToUserNotificationResponse>()
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => 
                    src.Receiver != null ? (src.Receiver.FirstName ?? "") + " " + (src.Receiver.LastName ?? "") : ""))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

            // Notification -> SendToMultipleUsersNotificationResponse
            CreateMap<Notification, SendToMultipleUsersNotificationResponse>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

            // Notification -> SendBroadcastNotificationResponse
            CreateMap<Notification, SendBroadcastNotificationResponse>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));
        }

    }
}
