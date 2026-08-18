using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventOrganizer.Infrastructure.Persistance.Seeding
{
    public sealed class DemoDataSeeder
    {
        public const string AdminEmail = "admin.demo@eventorganizer.local";
        public const string OrganizerEmail = "organizer.demo@eventorganizer.local";
        public const string ParticipantEmail = "participant.demo@eventorganizer.local";
        public const string SecondParticipantEmail = "participant2.demo@eventorganizer.local";
        public const string ThirdParticipantEmail = "participant3.demo@eventorganizer.local";
        public const string FourthParticipantEmail = "participant4.demo@eventorganizer.local";

        private readonly AppDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DemoDataSettings _settings;

        public DemoDataSeeder(
            AppDbContext dbContext,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IOptions<DemoDataSettings> settings)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _userManager = userManager;
            _settings = settings.Value;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "Demo data password must be configured when demo data is enabled.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var now = DateTime.UtcNow;
            var existingOrganizer = await _userManager.FindByEmailAsync(OrganizerEmail);
            if (existingOrganizer is not null)
            {
                await CompleteExistingDemoScenarioAsync(
                    existingOrganizer,
                    now,
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var admin = await CreateUserAsync(
                AdminEmail,
                "Aleksandar Marković",
                [ApplicationRoles.Admin]);
            var organizer = await CreateUserAsync(
                OrganizerEmail,
                "Marija Stojanović",
                [ApplicationRoles.Participant, ApplicationRoles.Organizer]);
            var participant = await CreateUserAsync(
                ParticipantEmail,
                "Ana Popović",
                [ApplicationRoles.Participant]);
            var secondParticipant = await CreateUserAsync(
                SecondParticipantEmail,
                "Marko Ilić",
                [ApplicationRoles.Participant]);
            var thirdParticipant = await CreateUserAsync(
                ThirdParticipantEmail,
                "Jelena Savić",
                [ApplicationRoles.Participant]);
            var fourthParticipant = await CreateUserAsync(
                FourthParticipantEmail,
                "Nikola Pavlović",
                [ApplicationRoles.Participant]);

            var resources = CreateResources(now);
            _dbContext.Resources.AddRange(resources.All);

            var planningEvent = Event.Create(
                "AI i razvoj modernih aplikacija",
                "Praktična radionica o primeni AI alata u projektovanju i razvoju savremenih veb aplikacija.",
                now.AddDays(14),
                now.AddDays(14).AddHours(4),
                80,
                260000m,
                "IT",
                2,
                organizer.Id,
                now,
                requiresEquipment: true);
            var planningBooking = EventResourceBooking.Create(planningEvent.Id, now);

            var submittedEvent = Event.Create(
                "Cloud Native Meetup",
                "Stručni meetup posvećen cloud platformama, kontejnerizaciji i razvoju skalabilnih aplikacija.",
                now.AddDays(21),
                now.AddDays(21).AddHours(3),
                50,
                120000m,
                "IT",
                1,
                organizer.Id,
                now);
            var submittedBooking = EventResourceBooking.Create(submittedEvent.Id, now);
            submittedBooking.AddResource(resources.WorkshopRoom.Id, ResourceType.Venue, now);
            submittedBooking.AddResource(resources.SecondItSpeaker.Id, ResourceType.Speaker, now);
            submittedBooking.Submit(now, now.AddHours(48));

            var publishedEvent = Event.Create(
                "Frontend konferencija",
                "Jednodnevna konferencija o modernim frontend tehnologijama, arhitekturi i korisničkom iskustvu.",
                now.AddDays(7),
                now.AddDays(7).AddHours(5),
                100,
                230000m,
                "IT",
                1,
                organizer.Id,
                now.AddDays(-10),
                requiresEquipment: true);
            var publishedBooking = CreateApprovedBooking(
                publishedEvent,
                admin.Id,
                now.AddDays(-9),
                resources.MainHall,
                [resources.MainItSpeaker],
                resources.ProfessionalEquipment);
            publishedEvent.Publish(now.AddDays(-8));

            var completedStartsAtUtc = now.AddDays(-14);
            var completedEvent = Event.Create(
                "Clean Architecture u praksi",
                "Predavanje sa praktičnim primerima organizovanja aplikacija prema principima Clean Architecture.",
                completedStartsAtUtc,
                completedStartsAtUtc.AddHours(4),
                60,
                110000m,
                "IT",
                1,
                organizer.Id,
                now.AddDays(-30));
            var completedBooking = CreateApprovedBooking(
                completedEvent,
                admin.Id,
                now.AddDays(-20),
                resources.WorkshopRoom,
                [resources.SecondItSpeaker]);
            completedEvent.Publish(now.AddDays(-19));
            completedEvent.Complete(now.AddDays(-13));

            var cancelledEvent = Event.Create(
                "Otkazana radionica digitalnog marketinga",
                "Radionica o planiranju digitalnih kampanja, kreiranju sadržaja i analizi rezultata.",
                now.AddDays(30),
                now.AddDays(30).AddHours(3),
                40,
                140000m,
                "Marketing",
                1,
                organizer.Id,
                now.AddDays(-10));
            var cancelledBooking = EventResourceBooking.Create(
                cancelledEvent.Id,
                now.AddDays(-10));
            cancelledBooking.Cancel(now.AddDays(-3));
            cancelledEvent.Cancel(now.AddDays(-3));

            _dbContext.Events.AddRange(
                planningEvent,
                submittedEvent,
                publishedEvent,
                completedEvent,
                cancelledEvent);
            _dbContext.EventResourceBookings.AddRange(
                planningBooking,
                submittedBooking,
                publishedBooking,
                completedBooking,
                cancelledBooking);

            _dbContext.Registrations.AddRange(CreatePublishedEventRegistrations(
                publishedEvent.Id,
                organizer.Id,
                participant.Id,
                secondParticipant.Id,
                thirdParticipant.Id,
                fourthParticipant.Id,
                now));
            _dbContext.Registrations.AddRange(CreateCompletedEventRegistrations(
                completedEvent.Id,
                organizer.Id,
                participant.Id,
                secondParticipant.Id,
                thirdParticipant.Id,
                now));
            _dbContext.Registrations.AddRange(CreateCancelledEventRegistrations(
                cancelledEvent.Id,
                organizer.Id,
                participant.Id,
                secondParticipant.Id,
                now));

            var roleRequests = CreateOrganizerRoleRequests(
                organizer.Id,
                fourthParticipant.Id,
                admin.Id,
                now);
            _dbContext.OrganizerRoleRequests.AddRange(
                roleRequests.Approved,
                roleRequests.Rejected);

            _dbContext.Reviews.AddRange(
                Review.Create(
                    completedEvent.Id,
                    participant.Id,
                    5,
                    "Odlična organizacija i veoma korisni praktični primeri.",
                    now.AddDays(-12)),
                Review.Create(
                    completedEvent.Id,
                    secondParticipant.Id,
                    4,
                    "Kvalitetno predavanje i dobra diskusija sa učesnicima.",
                    now.AddDays(-11)));

            await SeedNotificationsAsync(
                organizer.Id,
                participant.Id,
                secondParticipant.Id,
                thirdParticipant.Id,
                fourthParticipant.Id,
                publishedEvent,
                completedEvent,
                cancelledEvent,
                roleRequests,
                now,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private async Task<ApplicationUser> CreateUserAsync(
            string email,
            string fullName,
            IReadOnlyCollection<string> roles)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(user, _settings.Password);
            EnsureSucceeded(createResult, $"create demo user '{email}'");

            var roleResult = await _userManager.AddToRolesAsync(user, roles);
            EnsureSucceeded(roleResult, $"assign roles to demo user '{email}'");

            return user;
        }

        private static DemoResources CreateResources(DateTime now)
        {
            var mainHall = Venue.Create(
                "Konferencijska sala Aurora",
                "Velika sala sa binom i konferencijskim rasporedom; cena obuhvata angažovanje za jedan događaj.",
                90000m,
                120,
                5,
                now);
            var workshopRoom = Venue.Create(
                "Radioničarska sala Studio",
                "Fleksibilna sala za radionice i meetup događaje; cena obuhvata angažovanje za jedan događaj.",
                45000m,
                60,
                4,
                now);
            var archivedVenue = Venue.Create(
                "Sala Panorama",
                "Manja konferencijska sala sa prirodnim osvetljenjem i osnovnom prezentacionom opremom.",
                30000m,
                40,
                3,
                now);
            archivedVenue.Archive(now);

            var mainItSpeaker = Speaker.Create(
                "Milica Petrović",
                "Predavač specijalizovan za frontend arhitekturu i AI alate; cena predstavlja angažovanje na događaju.",
                45000m,
                "IT",
                5,
                now);
            var secondItSpeaker = Speaker.Create(
                "Stefan Jovanović",
                "Predavač iz oblasti cloud platformi i Clean Architecture pristupa; cena predstavlja angažovanje na događaju.",
                38000m,
                "IT",
                4,
                now);
            var marketingSpeaker = Speaker.Create(
                "Ivana Nikolić",
                "Predavač za digitalni marketing i komunikacione strategije; cena predstavlja angažovanje na događaju.",
                42000m,
                "Marketing",
                5,
                now);
            var unavailableSpeaker = Speaker.Create(
                "Nemanja Ilić",
                "DevOps inženjer specijalizovan za automatizaciju isporuke i cloud infrastrukturu.",
                40000m,
                "IT",
                4,
                now);
            unavailableSpeaker.MarkUnavailable(now);

            var professionalEquipment = EquipmentPackage.Create(
                "Profesionalni AV paket",
                "Kompletna audio-video podrška za jedan konferencijski događaj.",
                65000m,
                "EventTech Serbia",
                150,
                "IT",
                true,
                "Ozvučenje, projektor, dva mikrofona, rasveta i tehnička podrška.",
                5,
                now);
            var basicEquipment = EquipmentPackage.Create(
                "Osnovni prezentacioni paket",
                "Osnovna prezentaciona oprema za jedan manji događaj.",
                25000m,
                "Studio AV",
                60,
                "IT",
                false,
                "Projektor, platno i jedan bežični mikrofon.",
                4,
                now);
            var marketingEquipment = EquipmentPackage.Create(
                "Promo produkcijski paket",
                "Oprema namenjena jednoj marketing prezentaciji i snimanju sadržaja.",
                55000m,
                "Media Lab",
                80,
                "Marketing",
                true,
                "LED rasveta, kamera, ozvučenje i operater.",
                5,
                now);

            return new DemoResources(
                mainHall,
                workshopRoom,
                mainItSpeaker,
                secondItSpeaker,
                professionalEquipment,
                [
                    mainHall,
                    workshopRoom,
                    archivedVenue,
                    mainItSpeaker,
                    secondItSpeaker,
                    marketingSpeaker,
                    unavailableSpeaker,
                    professionalEquipment,
                    basicEquipment,
                    marketingEquipment,
                ]);
        }

        private static EventResourceBooking CreateApprovedBooking(
            Event eventItem,
            Guid adminUserId,
            DateTime submittedAtUtc,
            Venue venue,
            IReadOnlyCollection<Speaker> speakers,
            EquipmentPackage? equipmentPackage = null)
        {
            var booking = EventResourceBooking.Create(eventItem.Id, submittedAtUtc);
            booking.AddResource(venue.Id, ResourceType.Venue, submittedAtUtc);
            foreach (var speaker in speakers)
            {
                booking.AddResource(speaker.Id, ResourceType.Speaker, submittedAtUtc);
            }

            if (equipmentPackage is not null)
            {
                booking.AddResource(
                    equipmentPackage.Id,
                    ResourceType.EquipmentPackage,
                    submittedAtUtc);
            }

            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));
            booking.Approve(adminUserId, submittedAtUtc.AddHours(1));
            return booking;
        }

        private static IReadOnlyCollection<Registration> CreatePublishedEventRegistrations(
            Guid eventId,
            Guid organizerUserId,
            Guid participantUserId,
            Guid secondParticipantUserId,
            Guid thirdParticipantUserId,
            Guid fourthParticipantUserId,
            DateTime now)
        {
            var pending = Registration.Create(eventId, participantUserId, now.AddDays(-2));
            var confirmed = Registration.Create(eventId, secondParticipantUserId, now.AddDays(-2));
            confirmed.Confirm(organizerUserId, now.AddDays(-1));
            var rejected = Registration.Create(eventId, thirdParticipantUserId, now.AddDays(-2));
            rejected.Reject(
                "Prijava nije mogla biti potvrđena u trenutnom terminu.",
                organizerUserId,
                now.AddDays(-1));
            var cancelled = Registration.Create(eventId, fourthParticipantUserId, now.AddDays(-2));
            cancelled.Cancel(now.AddDays(-1));

            return [pending, confirmed, rejected, cancelled];
        }

        private static IReadOnlyCollection<Registration> CreateCompletedEventRegistrations(
            Guid eventId,
            Guid organizerUserId,
            Guid participantUserId,
            Guid secondParticipantUserId,
            Guid thirdParticipantUserId,
            DateTime now)
        {
            var firstConfirmed = Registration.Create(eventId, participantUserId, now.AddDays(-20));
            firstConfirmed.Confirm(organizerUserId, now.AddDays(-19));
            var secondConfirmed = Registration.Create(
                eventId,
                secondParticipantUserId,
                now.AddDays(-20));
            secondConfirmed.Confirm(organizerUserId, now.AddDays(-19));
            var cancelled = Registration.Create(eventId, thirdParticipantUserId, now.AddDays(-20));
            cancelled.Cancel(now.AddDays(-18));

            return [firstConfirmed, secondConfirmed, cancelled];
        }

        private static IReadOnlyCollection<Registration> CreateCancelledEventRegistrations(
            Guid eventId,
            Guid organizerUserId,
            Guid participantUserId,
            Guid secondParticipantUserId,
            DateTime now)
        {
            var pending = Registration.Create(eventId, participantUserId, now.AddDays(-5));
            pending.Cancel(now.AddDays(-3));

            var confirmed = Registration.Create(
                eventId,
                secondParticipantUserId,
                now.AddDays(-6));
            confirmed.Confirm(organizerUserId, now.AddDays(-5));
            confirmed.Cancel(now.AddDays(-3));

            return [pending, confirmed];
        }

        private static DemoRoleRequests CreateOrganizerRoleRequests(
            Guid organizerUserId,
            Guid rejectedUserId,
            Guid adminUserId,
            DateTime now)
        {
            var approved = OrganizerRoleRequest.Create(
                organizerUserId,
                "Želim da organizujem stručne IT događaje i praktične radionice za lokalnu zajednicu.",
                now.AddDays(-90));
            approved.Approve(adminUserId, now.AddDays(-89));

            var rejected = OrganizerRoleRequest.Create(
                rejectedUserId,
                "Želim da isprobam organizovanje događaja.",
                now.AddDays(-8));
            rejected.Reject(
                adminUserId,
                "Potrebno je navesti konkretnije iskustvo i plan događaja.",
                now.AddDays(-7));

            return new DemoRoleRequests(approved, rejected);
        }

        private async Task CompleteExistingDemoScenarioAsync(
            ApplicationUser organizer,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var admin = await GetRequiredDemoUserAsync(AdminEmail);
            var participant = await GetRequiredDemoUserAsync(ParticipantEmail);
            var secondParticipant = await GetRequiredDemoUserAsync(SecondParticipantEmail);
            var thirdParticipant = await GetRequiredDemoUserAsync(ThirdParticipantEmail);
            var fourthParticipant = await GetRequiredDemoUserAsync(FourthParticipantEmail);

            var publishedEvent = await GetRequiredDemoEventAsync("Frontend konferencija", cancellationToken);
            var completedEvent = await GetRequiredDemoEventAsync(
                "Clean Architecture u praksi",
                cancellationToken);
            var cancelledEvent = await GetRequiredDemoEventAsync(
                "Otkazana radionica digitalnog marketinga",
                cancellationToken);

            await EnsureCancelledRegistrationAsync(
                cancelledEvent.Id,
                participant.Id,
                now,
                cancellationToken);
            await EnsureCancelledRegistrationAsync(
                cancelledEvent.Id,
                secondParticipant.Id,
                now,
                cancellationToken);

            var approvedRequest = await _dbContext.OrganizerRoleRequests
                .FirstOrDefaultAsync(
                    request => request.UserId == organizer.Id
                        && request.Status == OrganizerRoleRequestStatus.Approved,
                    cancellationToken);
            if (approvedRequest is null)
            {
                approvedRequest = OrganizerRoleRequest.Create(
                    organizer.Id,
                    "Želim da organizujem stručne IT događaje i praktične radionice za lokalnu zajednicu.",
                    now.AddDays(-90));
                approvedRequest.Approve(admin.Id, now.AddDays(-89));
                _dbContext.OrganizerRoleRequests.Add(approvedRequest);
            }

            var rejectedRequest = await _dbContext.OrganizerRoleRequests
                .FirstOrDefaultAsync(
                    request => request.UserId == fourthParticipant.Id
                        && request.Status == OrganizerRoleRequestStatus.Rejected,
                    cancellationToken);
            if (rejectedRequest is null)
            {
                rejectedRequest = OrganizerRoleRequest.Create(
                    fourthParticipant.Id,
                    "Želim da isprobam organizovanje događaja.",
                    now.AddDays(-8));
                rejectedRequest.Reject(
                    admin.Id,
                    "Potrebno je navesti konkretnije iskustvo i plan događaja.",
                    now.AddDays(-7));
                _dbContext.OrganizerRoleRequests.Add(rejectedRequest);
            }

            await SeedNotificationsAsync(
                organizer.Id,
                participant.Id,
                secondParticipant.Id,
                thirdParticipant.Id,
                fourthParticipant.Id,
                publishedEvent,
                completedEvent,
                cancelledEvent,
                new DemoRoleRequests(approvedRequest, rejectedRequest),
                now,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureCancelledRegistrationAsync(
            Guid eventId,
            Guid participantUserId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var registration = await _dbContext.Registrations.SingleOrDefaultAsync(
                item => item.EventId == eventId
                    && item.ParticipantUserId == participantUserId,
                cancellationToken);
            if (registration is not null)
            {
                return;
            }

            registration = Registration.Create(eventId, participantUserId, now.AddDays(-5));
            registration.Cancel(now.AddDays(-3));
            _dbContext.Registrations.Add(registration);
        }

        private async Task SeedNotificationsAsync(
            Guid organizerUserId,
            Guid participantUserId,
            Guid secondParticipantUserId,
            Guid thirdParticipantUserId,
            Guid fourthParticipantUserId,
            Event publishedEvent,
            Event completedEvent,
            Event cancelledEvent,
            DemoRoleRequests roleRequests,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var notifications = await _dbContext.Notifications.ToListAsync(cancellationToken);

            AddNotificationIfMissing(
                notifications,
                organizerUserId,
                NotificationType.OrganizerRoleRequestApproved,
                NotificationRelatedEntityType.OrganizerRoleRequest,
                roleRequests.Approved.Id,
                now.AddDays(-89),
                markAsRead: true,
                () => _notificationService.AddOrganizerRoleRequestApproved(
                    organizerUserId,
                    roleRequests.Approved.Id,
                    now.AddDays(-89)));
            AddNotificationIfMissing(
                notifications,
                fourthParticipantUserId,
                NotificationType.OrganizerRoleRequestRejected,
                NotificationRelatedEntityType.OrganizerRoleRequest,
                roleRequests.Rejected.Id,
                now.AddDays(-7),
                markAsRead: false,
                () => _notificationService.AddOrganizerRoleRequestRejected(
                    fourthParticipantUserId,
                    roleRequests.Rejected.Id,
                    roleRequests.Rejected.DecisionReason!,
                    now.AddDays(-7)));
            AddNotificationIfMissing(
                notifications,
                organizerUserId,
                NotificationType.BookingApproved,
                NotificationRelatedEntityType.Event,
                publishedEvent.Id,
                now.AddDays(-8),
                markAsRead: true,
                () => _notificationService.AddBookingApproved(
                    organizerUserId,
                    publishedEvent.Id,
                    publishedEvent.Title,
                    now.AddDays(-8)));
            AddNotificationIfMissing(
                notifications,
                secondParticipantUserId,
                NotificationType.RegistrationConfirmed,
                NotificationRelatedEntityType.Event,
                publishedEvent.Id,
                now.AddDays(-1),
                markAsRead: true,
                () => _notificationService.AddRegistrationConfirmed(
                    secondParticipantUserId,
                    publishedEvent.Id,
                    publishedEvent.Title,
                    now.AddDays(-1)));
            AddNotificationIfMissing(
                notifications,
                thirdParticipantUserId,
                NotificationType.RegistrationRejected,
                NotificationRelatedEntityType.Event,
                publishedEvent.Id,
                now.AddDays(-1),
                markAsRead: false,
                () => _notificationService.AddRegistrationRejected(
                    thirdParticipantUserId,
                    publishedEvent.Id,
                    publishedEvent.Title,
                    "Prijava nije mogla biti potvrđena u trenutnom terminu.",
                    now.AddDays(-1)));
            AddNotificationIfMissing(
                notifications,
                organizerUserId,
                NotificationType.RegistrationCancelled,
                NotificationRelatedEntityType.Event,
                publishedEvent.Id,
                now.AddDays(-1),
                markAsRead: false,
                () => _notificationService.AddRegistrationCancelled(
                    organizerUserId,
                    publishedEvent.Id,
                    publishedEvent.Title,
                    now.AddDays(-1)));

            foreach (var recipientUserId in new[] { participantUserId, secondParticipantUserId })
            {
                AddNotificationIfMissing(
                    notifications,
                    recipientUserId,
                    NotificationType.EventCancelled,
                    NotificationRelatedEntityType.Event,
                    cancelledEvent.Id,
                    now.AddDays(-3),
                    markAsRead: false,
                    () => _notificationService.AddEventCancelled(
                        [recipientUserId],
                        cancelledEvent.Id,
                        cancelledEvent.Title,
                        now.AddDays(-3)));
                AddNotificationIfMissing(
                    notifications,
                    recipientUserId,
                    NotificationType.ReviewAvailable,
                    NotificationRelatedEntityType.Event,
                    completedEvent.Id,
                    now.AddDays(-13),
                    markAsRead: true,
                    () => _notificationService.AddReviewAvailable(
                        [recipientUserId],
                        completedEvent.Id,
                        completedEvent.Title,
                        now.AddDays(-13)));
            }
        }

        private void AddNotificationIfMissing(
            ICollection<Notification> notifications,
            Guid recipientUserId,
            NotificationType type,
            NotificationRelatedEntityType relatedEntityType,
            Guid relatedEntityId,
            DateTime createdAtUtc,
            bool markAsRead,
            Action addNotification)
        {
            var notification = notifications.FirstOrDefault(item =>
                item.RecipientUserId == recipientUserId
                && item.Type == type
                && item.RelatedEntityType == relatedEntityType
                && item.RelatedEntityId == relatedEntityId);

            if (notification is null)
            {
                addNotification();
                notification = _dbContext.Notifications.Local.Last(item =>
                    item.RecipientUserId == recipientUserId
                    && item.Type == type
                    && item.RelatedEntityType == relatedEntityType
                    && item.RelatedEntityId == relatedEntityId);
                notifications.Add(notification);
            }

            if (markAsRead && !notification.IsRead)
            {
                notification.MarkAsRead(createdAtUtc.AddHours(2));
            }
        }

        private async Task<ApplicationUser> GetRequiredDemoUserAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException($"Demo user '{email}' was not found.");
        }

        private async Task<Event> GetRequiredDemoEventAsync(
            string title,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Events.SingleOrDefaultAsync(
                    eventItem => eventItem.Title == title,
                    cancellationToken)
                ?? throw new InvalidOperationException($"Demo event '{title}' was not found.");
        }

        private static void EnsureSucceeded(IdentityResult result, string operation)
        {
            if (result.Succeeded)
            {
                return;
            }

            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to {operation}. Errors: {errors}");
        }

        private sealed record DemoResources(
            Venue MainHall,
            Venue WorkshopRoom,
            Speaker MainItSpeaker,
            Speaker SecondItSpeaker,
            EquipmentPackage ProfessionalEquipment,
            IReadOnlyCollection<Resource> All);

        private sealed record DemoRoleRequests(
            OrganizerRoleRequest Approved,
            OrganizerRoleRequest Rejected);
    }
}
