using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using BulkyBook.DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;
using BulkyBook.DataAccess.DBInitializer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//add dbcontext to container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//configure cho stripe payment, lấy cấu hình stripe tự động tiêm vào model Stripesettings cho 2 khóa
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));


//add identityuser và thêm role để có thể nhận biết role của user
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

// thêm cấu hình mặc định cho các routine
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";

});

//thêm cấu hình xác thực facebook
builder.Services.AddAuthentication().AddFacebook(option =>
{
    option.AppId = "1366649127645782";
    option.AppSecret = "6ed78e847266367310f1650eb5e9ac59";
});

//thêm cấu hình cho session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//them service cho razorpage
builder.Services.AddRazorPages();
//đăng ký dependency injection cho unitofwork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//add DI cho emailsender
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IDBInitializer, DBInitializer>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

//configure stripe
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

app.UseRouting();

//xác thực authen phải trước ủy quyền khi tạo thêm identity, xác thực user, pass trước khi ủy quyền là role gì
app.UseAuthentication();
app.UseAuthorization();

//thêm đường dẫn session
app.UseSession();

SeedDatabase();

//thêm map cho endpoint razorpage
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();
//seed databse từ dbinitializer
void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
        dbInitializer.Initialize();
    }
}
