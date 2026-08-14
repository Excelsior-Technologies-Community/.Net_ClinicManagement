using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;

namespace ClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment env;
        string cs = @"Data Source=DESKTOP-J2OEC9R\SQLEXPRESS02;Initial Catalog=ClinicDB;Integrated Security=True;TrustServerCertificate=True";
    

        public AdminController(IWebHostEnvironment env)
        {
            this.env = env;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminId") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(AdminModel model)
        {
            
            ModelState.Remove("FullName");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("MobileNo");
            ModelState.Remove("ImageFile");
            ModelState.Remove("ProfileImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT AdminId, FullName, Email
            FROM tbl_Admin
            WHERE Email = @Email
            AND Password = @Password
            AND IsActive = 1
            AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@Email", model.Email!);
                cmd.Parameters.AddWithValue("@Password", model.Password!);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    HttpContext.Session.SetString("AdminId", dr["AdminId"].ToString()!);
                    HttpContext.Session.SetString("FullName", dr["FullName"].ToString()!);
                    HttpContext.Session.SetString("Email", dr["Email"].ToString()!);

                    return RedirectToAction("Dashboard");
                }
            }

            ViewBag.Error = "Invalid Email or Password";
            return View(model);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }
        [HttpGet]
        public IActionResult ManageDepartment(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DepartmentModel> list = new List<DepartmentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"SELECT *
                         FROM tbl_Department
                         WHERE IsDeleted = 0
                         AND DepartmentName LIKE @Search
                         ORDER BY DisplayOrder ASC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    DepartmentModel model = new DepartmentModel();

                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                con.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
      
        [HttpGet]
        public IActionResult AddDepartment()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDepartment(DepartmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DepartmentImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                 
                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM tbl_Department WHERE DepartmentName=@DepartmentName AND IsDeleted=0",
                        con);

                    checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "Department already exists.";
                        return View(model);
                    }

                    // Image Upload
                    string fileName = "";

                    if (model.ImageFile != null)
                    {
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(),
                                                         "wwwroot",
                                                         "Department");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string filePath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    SqlCommand cmd = new SqlCommand(@"
            INSERT INTO tbl_Department
            (
                DepartmentName,
                DepartmentDescription,
                DepartmentImage,
                DisplayOrder,
                IsActive,
                IsDeleted,
                CreatedDate
            )
            VALUES
            (
                @DepartmentName,
                @DepartmentDescription,
                @DepartmentImage,
                @DisplayOrder,
                1,
                0,
                GETDATE()
            )", con);

                    cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    cmd.Parameters.AddWithValue("@DepartmentDescription", model.DepartmentDescription);
                    cmd.Parameters.AddWithValue("@DepartmentImage", fileName);
                    cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Department added successfully.";

                    return RedirectToAction("ManageDepartment");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DepartmentModel model = new DepartmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_Department WHERE DepartmentId=@DepartmentId AND IsDeleted=0", con);

                cmd.Parameters.AddWithValue("@DepartmentId", id);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }

                con.Close();
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDepartment(DepartmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DepartmentImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                   
                    SqlCommand checkCmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM tbl_Department
                  WHERE DepartmentName=@DepartmentName
                  AND DepartmentId<>@DepartmentId
                  AND IsDeleted=0", con);

                    checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "Department already exists.";
                        return View(model);
                    }

                    string oldImage = "";

                    SqlCommand imgCmd = new SqlCommand(
                        "SELECT DepartmentImage FROM tbl_Department WHERE DepartmentId=@DepartmentId", con);

                    imgCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    object obj = imgCmd.ExecuteScalar();

                    if (obj != null)
                    {
                        oldImage = obj.ToString();
                    }

                    string fileName = oldImage;

                    // Upload New Image
                    if (model.ImageFile != null)
                    {
                        string folderPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "Department");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        // Delete Old Image
                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            string oldPath = Path.Combine(folderPath, oldImage);

                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string newPath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(newPath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    // Update Record
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET DepartmentName=@DepartmentName,
                    DepartmentDescription=@DepartmentDescription,
                    DepartmentImage=@DepartmentImage,
                    DisplayOrder=@DisplayOrder,
                    UpdatedDate=GETDATE()
                WHERE DepartmentId=@DepartmentId", con);

                    cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    cmd.Parameters.AddWithValue("@DepartmentDescription", model.DepartmentDescription);
                    cmd.Parameters.AddWithValue("@DepartmentImage", fileName);
                    cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);
                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Department updated successfully.";

                    return RedirectToAction("ManageDepartment");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
      
        [HttpGet]
        public IActionResult ViewDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DepartmentModel model = new DepartmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT *
            FROM tbl_Department
            WHERE DepartmentId = @DepartmentId
            AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DepartmentId", id);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    TempData["Error"] = "Department not found.";
                    return RedirectToAction("ManageDepartment");
                }

                con.Close();
            }

            return View(model);
        }
    
        [HttpGet]
        public IActionResult DeleteDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET IsDeleted = 1,
                    UpdatedDate = GETDATE()
                WHERE DepartmentId = @DepartmentId", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", id);

                    con.Open();

                    int result = cmd.ExecuteNonQuery();

                    con.Close();

                    if (result > 0)
                    {
                        TempData["Success"] = "Department deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Department not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDepartment");
        }
        
        [HttpGet]
        public IActionResult ChangeDepartmentStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT IsActive FROM tbl_Department WHERE DepartmentId=@DepartmentId AND IsDeleted=0",
                        con);

                    checkCmd.Parameters.AddWithValue("@DepartmentId", id);

                    object result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        TempData["Error"] = "Department not found.";
                        return RedirectToAction("ManageDepartment");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                   
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET IsActive=@IsActive,
                    UpdatedDate=GETDATE()
                WHERE DepartmentId=@DepartmentId", con);

                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@DepartmentId", id);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = newStatus
                        ? "Department activated successfully."
                        : "Department deactivated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDepartment");
        }
        [HttpGet]
        public IActionResult ManageDoctor(string search = "", long departmentId = 0)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DoctorModel> doctorList = new List<DoctorModel>();


            ViewBag.DepartmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

           
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY DepartmentName",
                    con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                List<SelectListItem> departments = new List<SelectListItem>();

                departments.Add(new SelectListItem
                {
                    Text = "-- All Departments --",
                    Value = "0"
                });

                while (deptDr.Read())
                {
                    departments.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();

                ViewBag.DepartmentList = departments;

                // Doctor List
                SqlCommand cmd = new SqlCommand(@"
     SELECT
         D.DoctorId,
         D.DepartmentId,
         DP.DepartmentName,
         D.DoctorName,
         D.Qualification,
         D.Specialization,
         D.Experience,
         D.ConsultationFee,
         D.MobileNo,
         D.Email,
         D.Gender,
         D.DoctorImage,
         D.AvailableFrom,
         D.AvailableTo,
         D.Description,
         D.IsActive,
         D.CreatedDate,
         D.UpdatedDate
     FROM tbl_Doctor D
     INNER JOIN tbl_Department DP
         ON D.DepartmentId = DP.DepartmentId
     WHERE D.IsDeleted = 0
         AND (@Search = '' OR D.DoctorName LIKE '%' + @Search + '%')
         AND (@DepartmentId = 0 OR D.DepartmentId = @DepartmentId)
     ORDER BY D.DoctorId DESC", con);

                cmd.Parameters.AddWithValue("@Search", search);
                cmd.Parameters.AddWithValue("@DepartmentId", departmentId);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    doctorList.Add(new DoctorModel
                    {
                        DoctorId = Convert.ToInt64(dr["DoctorId"]),
                        DepartmentId = Convert.ToInt64(dr["DepartmentId"]),
                        DepartmentName = dr["DepartmentName"].ToString(),
                        DoctorName = dr["DoctorName"].ToString(),
                        Qualification = dr["Qualification"].ToString(),
                        Specialization = dr["Specialization"].ToString(),
                        Experience = Convert.ToInt32(dr["Experience"]),
                        ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]),
                        MobileNo = dr["MobileNo"].ToString(),
                        Email = dr["Email"].ToString(),
                        Gender = dr["Gender"].ToString(),
                        DoctorImage = dr["DoctorImage"].ToString(),
                        AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                            ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                            : null,
                        AvailableTo = dr["AvailableTo"] != DBNull.Value
                            ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
                            : null,
                        Description = dr["Description"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(dr["UpdatedDate"])
                    });
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.DepartmentId = departmentId;

            return View(doctorList);
        }
        [HttpGet]
        public IActionResult AddDoctor()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
     SELECT DepartmentId, DepartmentName
     FROM tbl_Department
     WHERE IsDeleted = 0
       AND IsActive = 1
     ORDER BY DepartmentName", con);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (dr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = dr["DepartmentId"].ToString(),
                        Text = dr["DepartmentName"].ToString()
                    });
                }

                dr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDoctor(DoctorModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

          
            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted=0 AND IsActive=1 ORDER BY DepartmentName",
                    con);

                con.Open();

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Duplicate Check
                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorName=@DoctorName
         AND DepartmentId=@DepartmentId
         AND IsDeleted=0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        TempData["Error"] = "Doctor already exists in this department.";
                        return View(model);
                    }

                    // Image Upload
                    string fileName = "";

                    if (model.ImageFile != null)
                    {
                        string folder = Path.Combine(Directory.GetCurrentDirectory(),
                            "wwwroot/Department");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string filePath = Path.Combine(folder, fileName);

                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fs);
                        }
                    }

                    // Insert
                    SqlCommand cmd = new SqlCommand(@"
     INSERT INTO tbl_Doctor
     (
         DepartmentId,
         DoctorName,
         Qualification,
         Specialization,
         Experience,
         ConsultationFee,
         MobileNo,
         Email,
         Gender,
         DoctorImage,
         AvailableFrom,
         AvailableTo,
         Description,
         IsActive,
         IsDeleted,
         CreatedDate
     )
     VALUES
     (
         @DepartmentId,
         @DoctorName,
         @Qualification,
         @Specialization,
         @Experience,
         @ConsultationFee,
         @MobileNo,
         @Email,
         @Gender,
         @DoctorImage,
         @AvailableFrom,
         @AvailableTo,
         @Description,
         1,
         0,
         GETDATE()
     )", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    cmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    cmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                    cmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@Experience", model.Experience);
                    cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@DoctorImage", fileName);
                    cmd.Parameters.AddWithValue("@AvailableFrom", model.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", model.AvailableTo);
                    cmd.Parameters.AddWithValue("@Description",
                        (object?)model.Description ?? DBNull.Value);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor added successfully.";
                }

                return RedirectToAction("ManageDoctor");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorModel model = new DoctorModel();


            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

           
                SqlCommand deptCmd = new SqlCommand(@"
     SELECT DepartmentId, DepartmentName
     FROM tbl_Department
     WHERE IsDeleted = 0
       AND IsActive = 1
     ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();

                ViewBag.DepartmentList = departmentList;

                // Load Doctor Details
                SqlCommand cmd = new SqlCommand(@"
     SELECT *
     FROM tbl_Doctor
     WHERE DoctorId = @DoctorId
       AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DoctorId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DoctorName = dr["DoctorName"].ToString();
                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = Convert.ToInt32(dr["Experience"]);
                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();
                    model.Gender = dr["Gender"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                        : null;

                    model.AvailableTo = dr["AvailableTo"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
                        : null;

                    model.Description = dr["Description"].ToString();
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("ManageDoctor");
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoctor(DoctorModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY DepartmentName",
                    con);

                con.Open();

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorName = @DoctorName
         AND DepartmentId = @DepartmentId
         AND DoctorId <> @DoctorId
         AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    checkCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        TempData["Error"] = "Doctor already exists in this department.";
                        return View(model);
                    }


                    string oldImage = "";

                    SqlCommand imgCmd = new SqlCommand(
                        "SELECT DoctorImage FROM tbl_Doctor WHERE DoctorId=@DoctorId",
                        con);

                    imgCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    object img = imgCmd.ExecuteScalar();

                    if (img != null)
                    {
                        oldImage = img.ToString();
                    }

                    string imageName = oldImage;


                    if (model.ImageFile != null)
                    {
                        string folder = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot/Department");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }


                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            string oldPath = Path.Combine(folder, oldImage);

                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        imageName = Guid.NewGuid().ToString()
                                    + Path.GetExtension(model.ImageFile.FileName);

                        string newPath = Path.Combine(folder, imageName);

                        using (FileStream fs = new FileStream(newPath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fs);
                        }
                    }


                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             DepartmentId=@DepartmentId,
             DoctorName=@DoctorName,
             Qualification=@Qualification,
             Specialization=@Specialization,
             Experience=@Experience,
             ConsultationFee=@ConsultationFee,
             MobileNo=@MobileNo,
             Email=@Email,
             Gender=@Gender,
             DoctorImage=@DoctorImage,
             AvailableFrom=@AvailableFrom,
             AvailableTo=@AvailableTo,
             Description=@Description,
             UpdatedDate=GETDATE()
         WHERE DoctorId=@DoctorId", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    cmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    cmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                    cmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@Experience", model.Experience);
                    cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@DoctorImage", imageName);
                    cmd.Parameters.AddWithValue("@AvailableFrom", model.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", model.AvailableTo);
                    cmd.Parameters.AddWithValue("@Description",
                        (object?)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor updated successfully.";
                }

                return RedirectToAction("ManageDoctor");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult ViewDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorModel model = new DoctorModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
     SELECT
         D.*,
         DP.DepartmentName
     FROM tbl_Doctor D
     INNER JOIN tbl_Department DP
         ON D.DepartmentId = DP.DepartmentId
     WHERE D.DoctorId = @DoctorId
     AND D.IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DoctorId", id);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DoctorName = dr["DoctorName"].ToString();
                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = Convert.ToInt32(dr["Experience"]);
                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();
                    model.Gender = dr["Gender"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();
                    model.Description = dr["Description"].ToString();

                    model.AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                        : null;

                    model.AvailableTo = dr["AvailableTo"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
                        : null;

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("ManageDoctor");
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult DeleteDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorId = @DoctorId
         AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorId", id);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count == 0)
                    {
                        TempData["Error"] = "Doctor not found.";
                        return RedirectToAction("ManageDoctor");
                    }

                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             IsDeleted = 1,
             UpdatedDate = GETDATE()
         WHERE DoctorId = @DoctorId", con);

                    cmd.Parameters.AddWithValue("@DoctorId", id);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDoctor");
        }
        [HttpGet]
        public IActionResult ChangeDoctorStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Check Doctor Exists & Get Current Status
                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT IsActive
         FROM tbl_Doctor
         WHERE DoctorId = @DoctorId
           AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorId", id);

                    object result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        TempData["Error"] = "Doctor not found.";
                        return RedirectToAction("ManageDoctor");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             IsActive = @IsActive,
             UpdatedDate = GETDATE()
         WHERE DoctorId = @DoctorId", con);

                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@DoctorId", id);

                    cmd.ExecuteNonQuery();

                    if (newStatus)
                    {
                        TempData["Success"] = "Doctor activated successfully.";
                    }
                    else
                    {
                        TempData["Success"] = "Doctor deactivated successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDoctor");
        }
      
        [HttpGet]
        public IActionResult ManagePatient(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<CustomerModel> patientList = new List<CustomerModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string query = @"
                    SELECT *
                    FROM tbl_Customer
                    WHERE IsDeleted = 0
                      AND (@Search = '' OR FullName LIKE '%' + @Search + '%' OR MobileNo LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
                    ORDER BY CustomerId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search);

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    patientList.Add(new CustomerModel
                    {
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
                        Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "",
                        DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null,
                        BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "",
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "",
                        City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "",
                        State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "",
                        Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "",
                        ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "",
                        IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]),
                        CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.Now,
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null
                    });
                }
                dr.Close();
            }

            ViewBag.Search = search;
            return View(patientList);
        }

        [HttpGet]
        public IActionResult ViewPatient(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            CustomerModel model = new CustomerModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0", con);
                cmd.Parameters.AddWithValue("@CustomerId", id);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "";
                    model.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    model.DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null;
                    model.BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "";
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "";
                    model.City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "";
                    model.State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "";
                    model.Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "";
                    model.ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "";
                    model.IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                    model.CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.Now;
                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();
                    TempData["Error"] = "Patient record not found.";
                    return RedirectToAction("ManagePatient");
                }
                dr.Close();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult DeletePatient(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_Customer 
                        SET IsDeleted = 1, UpdatedDate = GETDATE() 
                        WHERE CustomerId = @CustomerId", con);

                    cmd.Parameters.AddWithValue("@CustomerId", id);

                    int result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        TempData["Success"] = "Patient deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Patient record not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting patient: " + ex.Message;
            }

            return RedirectToAction("ManagePatient");
        }

        [HttpGet]
        public IActionResult ChangePatientStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    SqlCommand checkCmd = new SqlCommand("SELECT IsActive FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0", con);
                    checkCmd.Parameters.AddWithValue("@CustomerId", id);

                    object result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        TempData["Error"] = "Patient record not found.";
                        return RedirectToAction("ManagePatient");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                    SqlCommand cmd = new SqlCommand("UPDATE tbl_Customer SET IsActive = @IsActive, UpdatedDate = GETDATE() WHERE CustomerId = @CustomerId", con);
                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@CustomerId", id);
                    cmd.ExecuteNonQuery();

                    TempData["Success"] = newStatus ? "Patient activated successfully." : "Patient deactivated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManagePatient");
        }
   
        [HttpGet]
        public IActionResult ManageAppointments(string search = "", string status = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<AppointmentModel> list = new List<AppointmentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT
    a.AppointmentId,
    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,
    a.ConsultationFee,
    a.AppointmentStatus,
    a.PaymentStatus,
    a.CreatedDate,

    c.FullName AS CustomerName,
    c.MobileNo,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.IsDeleted = 0

AND (@Status = '' OR a.AppointmentStatus = @Status)

AND
(
    a.AppointmentNo LIKE @Search
    OR c.FullName LIKE @Search
    OR d.DoctorName LIKE @Search
)

ORDER BY

CASE
    WHEN a.AppointmentStatus='Pending' THEN 1
    WHEN a.AppointmentStatus='Approved' THEN 2
    WHEN a.AppointmentStatus='Confirmed' THEN 3
    WHEN a.AppointmentStatus='Completed' THEN 4
    WHEN a.AppointmentStatus='Cancelled' THEN 5
    WHEN a.AppointmentStatus='Rejected' THEN 6
    ELSE 7
END,

a.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    AppointmentModel model = new AppointmentModel();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.CustomerName = dr["CustomerName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.DoctorName = dr["DoctorName"].ToString();
                    model.Department = dr["DepartmentName"].ToString();
                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);
                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }
   
        [HttpGet]
        public IActionResult ViewAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.*,

    c.FullName AS CustomerName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.Qualification,
    d.Specialization,
    d.Experience,
    d.DoctorImage,
    d.AvailableFrom,
    d.AvailableTo,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = dr["Experience"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    if (dr["AvailableFrom"] != DBNull.Value)
                        model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];

                    if (dr["AvailableTo"] != DBNull.Value)
                        model.AvailableTo = (TimeSpan)dr["AvailableTo"];

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.Symptoms = dr["Symptoms"].ToString();

                    if (dr["ConsultationFee"] != DBNull.Value)
                        model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.AdminRemark = dr["AdminRemark"] == DBNull.Value ? "" : dr["AdminRemark"].ToString();
                    model.CustomerRemark = dr["CustomerRemark"] == DBNull.Value ? "" : dr["CustomerRemark"].ToString();
                    model.PrescriptionFile = dr["PrescriptionFile"] == DBNull.Value ? "" : dr["PrescriptionFile"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("ManageAppointments");
            }

            return View(model);
        }
        
        [HttpGet]
        public IActionResult EditAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.*,

    c.FullName AS CustomerName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = dr["Experience"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.AdminRemark = dr["AdminRemark"].ToString();

                    model.CustomerRemark = dr["CustomerRemark"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";

                return RedirectToAction("ManageAppointments");
            }

           
            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem(){ Text="Pending", Value="Pending"},
        new SelectListItem(){ Text="Approved", Value="Approved"},
        new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
        new SelectListItem(){ Text="Completed", Value="Completed"},
        new SelectListItem(){ Text="Rejected", Value="Rejected"},
        new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
    };

            return View(model);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAppointment(AppointmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

          
            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("Department");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("Qualification");
            ModelState.Remove("Specialization");
            ModelState.Remove("Experience");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("Prescription");
            ModelState.Remove("PrescriptionFile");
            ModelState.Remove("CustomerRemark");
            ModelState.Remove("AvailableFrom");
            ModelState.Remove("AvailableTo");
            ModelState.Remove("CustomerId");
            ModelState.Remove("DoctorId");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("Symptoms");
            ModelState.Remove("PaymentStatus");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedDate");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new List<SelectListItem>()
        {
            new SelectListItem(){ Text="Pending", Value="Pending"},
            new SelectListItem(){ Text="Approved", Value="Approved"},
            new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
            new SelectListItem(){ Text="Completed", Value="Completed"},
            new SelectListItem(){ Text="Rejected", Value="Rejected"},
            new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
        };

                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"UPDATE tbl_Appointment
                         SET
                            AppointmentDate=@AppointmentDate,
                            AppointmentTime=@AppointmentTime,
                            ConsultationFee=@ConsultationFee,
                            AppointmentStatus=@AppointmentStatus,
                            AdminRemark=@AdminRemark,
                            UpdatedDate=@UpdatedDate
                         WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                if (model.AppointmentDate != null)
                    cmd.Parameters.AddWithValue("@AppointmentDate", model.AppointmentDate);
                else
                    cmd.Parameters.AddWithValue("@AppointmentDate", DBNull.Value);

                if (model.AppointmentTime != null)
                    cmd.Parameters.AddWithValue("@AppointmentTime", model.AppointmentTime);
                else
                    cmd.Parameters.AddWithValue("@AppointmentTime", DBNull.Value);

                cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);

                cmd.Parameters.AddWithValue("@AppointmentStatus", model.AppointmentStatus);

                if (!string.IsNullOrWhiteSpace(model.AdminRemark))
                    cmd.Parameters.AddWithValue("@AdminRemark", model.AdminRemark);
                else
                    cmd.Parameters.AddWithValue("@AdminRemark", DBNull.Value);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment updated successfully.";
                    return RedirectToAction("ManageAppointments");
                }
                else
                {
                    TempData["Error"] = "Appointment update failed.";
                }
            }

            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem(){ Text="Pending", Value="Pending"},
        new SelectListItem(){ Text="Approved", Value="Approved"},
        new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
        new SelectListItem(){ Text="Completed", Value="Completed"},
        new SelectListItem(){ Text="Rejected", Value="Rejected"},
        new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
    };

            return View(model);
        }
       
        [HttpGet]
        public IActionResult ApproveAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

              
                string checkQuery = @"
        SELECT AppointmentStatus
        FROM tbl_Appointment
        WHERE AppointmentId=@AppointmentId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";

                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

                if (status == "Approved")
                {
                    TempData["Error"] = "Appointment is already approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Rejected")
                {
                    TempData["Error"] = "Rejected appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Completed")
                {
                    TempData["Error"] = "Completed appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                string query = @"
        UPDATE tbl_Appointment
        SET

            AppointmentStatus=@AppointmentStatus,
            UpdatedDate=@UpdatedDate

        WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Approved");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment approved successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to approve appointment.";
                }
            }

            return RedirectToAction("ManageAppointments");
        }
        
        [HttpGet]
        public IActionResult RejectAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                
                string checkQuery = @"
        SELECT AppointmentStatus
        FROM tbl_Appointment
        WHERE AppointmentId=@AppointmentId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";

                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

              
                if (status == "Rejected")
                {
                    TempData["Error"] = "Appointment is already rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Approved")
                {
                    TempData["Error"] = "Approved appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Completed")
                {
                    TempData["Error"] = "Completed appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                string query = @"
        UPDATE tbl_Appointment
        SET

            AppointmentStatus=@AppointmentStatus,
            UpdatedDate=@UpdatedDate

        WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Rejected");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment rejected successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to reject appointment.";
                }
            }

            return RedirectToAction("ManageAppointments");
        }
        
        [HttpGet]
        public IActionResult UploadPrescription(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.AppointmentId,
    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,
    a.PrescriptionFile,
    a.AppointmentStatus,

    c.FullName AS CustomerName,

    d.DoctorName,
    d.DoctorImage,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PrescriptionFile = dr["PrescriptionFile"] == DBNull.Value
                        ? ""
                        : dr["PrescriptionFile"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("ManageAppointments");
            }

           
            if (model.AppointmentStatus != "Approved")
            {
                TempData["Error"] = "Prescription can only be uploaded for approved appointments.";
                return RedirectToAction("ManageAppointments");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadPrescription(AppointmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DoctorName");
            ModelState.Remove("CustomerName");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("AppointmentStatus");
            ModelState.Remove("PrescriptionFile");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string fileName = "";

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand cmdOld = new SqlCommand("SELECT PrescriptionFile FROM tbl_Appointment WHERE AppointmentId=@AppointmentId", con);
                cmdOld.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                object oldFile = cmdOld.ExecuteScalar();

                if (oldFile != null && oldFile != DBNull.Value)
                {
                    fileName = oldFile.ToString();
                }

                if (model.Prescription != null && model.Prescription.Length > 0)
                {
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Prescription", fileName);

                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    string extension = Path.GetExtension(model.Prescription.FileName);

                    fileName = Guid.NewGuid().ToString() + extension;

                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Prescription");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string filePath = Path.Combine(folder, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.Prescription.CopyTo(stream);
                    }
                }

                SqlCommand cmd = new SqlCommand(@"
        UPDATE tbl_Appointment
        SET PrescriptionFile=@PrescriptionFile,
            UpdatedDate=@UpdatedDate
        WHERE AppointmentId=@AppointmentId", con);

                cmd.Parameters.AddWithValue("@PrescriptionFile", fileName);
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Prescription uploaded successfully.";
                }
                else
                {
                    TempData["Error"] = "Something went wrong.";
                }

                con.Close();
            }

            return RedirectToAction("ManageAppointments");
        }
        [HttpGet]
        public IActionResult CompleteAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand checkCmd = new SqlCommand("SELECT AppointmentStatus FROM tbl_Appointment WHERE AppointmentId=@AppointmentId", con);
                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";
                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

                if (status == "Completed")
                {
                    TempData["Error"] = "Appointment is already completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Rejected")
                {
                    TempData["Error"] = "Rejected appointment cannot be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status != "Approved")
                {
                    TempData["Error"] = "Only approved appointments can be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                SqlCommand cmd = new SqlCommand(@"UPDATE tbl_Appointment
                                          SET AppointmentStatus=@AppointmentStatus,
                                              UpdatedDate=@UpdatedDate
                                          WHERE AppointmentId=@AppointmentId", con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Completed");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Appointment completed successfully.";
            }

            return RedirectToAction("ManageAppointments");
        }
       
        [HttpGet]
        public IActionResult ManagePayments(string search = "", string status = "")
        {
      
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<PaymentModel> list = new List<PaymentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.PaymentId,
    p.AppointmentId,
    p.CustomerId,
    p.Amount,
    p.PaymentMethod,
    p.TransactionId,
    p.PaymentStatus,
    p.PaymentDate,
    p.CreatedDate,

    a.AppointmentNo,

    c.FullName,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
    ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
    ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE
(
    @Status=''
    OR
    p.PaymentStatus=@Status
)

AND
(
    a.AppointmentNo LIKE @Search
    OR
    c.FullName LIKE @Search
    OR
    d.DoctorName LIKE @Search
    OR
    ISNULL(p.TransactionId,'') LIKE @Search
)

ORDER BY p.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    PaymentModel model = new PaymentModel();

                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Amount = Convert.ToDecimal(dr["Amount"]);

                    model.PaymentMethod = dr["PaymentMethod"].ToString();

                    model.TransactionId = dr["TransactionId"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }
      
        [HttpGet]
        public IActionResult ViewPayment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PaymentModel model = new PaymentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.PaymentId,
    p.AppointmentId,
    p.CustomerId,
    p.Amount,
    p.PaymentMethod,
    p.TransactionId,
    p.PaymentStatus,
    p.PaymentDate,
    p.CreatedDate,
    p.UpdatedDate,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
    ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
    ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE p.PaymentId=@PaymentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PaymentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Amount = Convert.ToDecimal(dr["Amount"]);

                    model.PaymentMethod = dr["PaymentMethod"].ToString();

                    model.TransactionId = dr["TransactionId"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.PaymentId == 0)
            {
                TempData["Error"] = "Payment record not found.";

                return RedirectToAction("ManagePayments");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult DownloadReceipt(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PaymentModel model = new PaymentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.PaymentId,
    p.AppointmentId,
    p.CustomerId,
    p.Amount,
    p.PaymentMethod,
    p.TransactionId,
    p.PaymentStatus,
    p.PaymentDate,
    p.CreatedDate,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE p.PaymentId=@PaymentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PaymentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Amount = Convert.ToDecimal(dr["Amount"]);

                    model.PaymentMethod = dr["PaymentMethod"].ToString();
                    model.TransactionId = dr["TransactionId"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);
                }

                dr.Close();
            }

            if (model.PaymentId == 0)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction("ManagePayments");
            }

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Text("CLINIC MANAGEMENT SYSTEM")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            col.Item().Text("PAYMENT RECEIPT")
                                .FontSize(18)
                                .Bold();

                            col.Item().PaddingVertical(10);
                        });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text($"Receipt No : {model.PaymentId}");
                        col.Item().Text($"Appointment No : {model.AppointmentNo}");

                        col.Item().Text($"Customer Name : {model.CustomerName}");
                        col.Item().Text($"Mobile : {model.MobileNo}");
                        col.Item().Text($"Email : {model.Email}");

                        col.Item().Text($"Doctor : Dr. {model.DoctorName}");
                        col.Item().Text($"Department : {model.DepartmentName}");

                        col.Item().Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");
                        col.Item().Text($"Appointment Time : {model.AppointmentTime}");

                        col.Item().Text($"Amount Paid : ₹ {model.Amount:N2}");
                        col.Item().Text($"Payment Method : {model.PaymentMethod}");
                        col.Item().Text($"Transaction ID : {model.TransactionId}");
                        col.Item().Text($"Payment Status : {model.PaymentStatus}");
                        col.Item().Text($"Payment Date : {model.PaymentDate:dd MMM yyyy hh:mm tt}");

                        col.Item().PaddingTop(20);

                        col.Item().LineHorizontal(1);

                        col.Item().PaddingTop(20);

                        col.Item().AlignCenter().Text("Thank You")
                            .Bold()
                            .FontSize(18)
                            .FontColor(Colors.Green.Darken2);

                        col.Item().AlignCenter().Text("Clinic Management System");
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated On : {DateTime.Now:dd MMM yyyy hh:mm tt}");
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                "PaymentReceipt_" + model.PaymentId + ".pdf");
        }
      
        [HttpGet]
        public IActionResult ManagePrescriptions(string search = "", string status = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<PrescriptionModel> list = new List<PrescriptionModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.PrescriptionId,
    p.AppointmentId,
    p.CustomerId,
    p.DoctorId,
    p.Diagnosis,
    p.NextVisitDate,
    p.PrescriptionStatus,
    p.CreatedDate,

    a.AppointmentNo,
    a.AppointmentDate,

    c.FullName,
    c.MobileNo,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE
p.IsDeleted = 0

AND
(
    @Status = ''
    OR
    p.PrescriptionStatus = @Status
)

AND
(
       a.AppointmentNo LIKE @Search
    OR c.FullName LIKE @Search
    OR d.DoctorName LIKE @Search
    OR p.Diagnosis LIKE @Search
)

ORDER BY p.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    PrescriptionModel model = new PrescriptionModel();

                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }
        
        [HttpGet]
        public IActionResult AddPrescription(long appointmentId)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"
SELECT COUNT(*)
FROM tbl_Prescription
WHERE AppointmentId=@AppointmentId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Prescription already exists for this appointment.";
                    return RedirectToAction("ManagePrescriptions");
                }


                string query = @"
SELECT

    a.AppointmentId,
    a.AppointmentNo,
    a.CustomerId,
    a.DoctorId,
    a.AppointmentDate,
    a.AppointmentTime,
    a.AppointmentStatus,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE
a.AppointmentId=@AppointmentId
AND a.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();
                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = dr["Experience"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.PrescriptionStatus = "Active";
                    model.NextVisitDate = DateTime.Today.AddDays(7);

                    string appointmentStatus = dr["AppointmentStatus"].ToString();

                    dr.Close();


                    if (appointmentStatus != "Completed")
                    {
                        TempData["Error"] =
                            "Prescription can only be added after appointment is completed.";

                        return RedirectToAction("ManageAppointments");
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Appointment not found.";

                    return RedirectToAction("ManageAppointments");
                }
            }

            return View(model);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPrescription(PrescriptionModel model)
        {
           
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

           
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("CustomerName");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("DoctorName");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("Qualification");
            ModelState.Remove("Specialization");
            ModelState.Remove("Experience");
            ModelState.Remove("AppointmentDate");
            ModelState.Remove("AppointmentTime");
            ModelState.Remove("ClinicName");
            ModelState.Remove("ClinicAddress");
            ModelState.Remove("ClinicMobile");
            ModelState.Remove("ClinicEmail");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"
SELECT COUNT(*)
FROM tbl_Prescription
WHERE AppointmentId=@AppointmentId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Prescription already exists.";

                    return RedirectToAction("ManagePrescriptions");
                }


                string query = @"
INSERT INTO tbl_Prescription
(
    AppointmentId,
    CustomerId,
    DoctorId,
    Diagnosis,
    Symptoms,
    Medicines,
    Dosage,
    Instructions,
    NextVisitDate,
    PrescriptionStatus,
    CreatedDate,
    IsDeleted
)
VALUES
(
    @AppointmentId,
    @CustomerId,
    @DoctorId,
    @Diagnosis,
    @Symptoms,
    @Medicines,
    @Dosage,
    @Instructions,
    @NextVisitDate,
    @PrescriptionStatus,
    @CreatedDate,
    @IsDeleted
)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);

                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                cmd.Parameters.AddWithValue("@Diagnosis", model.Diagnosis);

                cmd.Parameters.AddWithValue("@Symptoms",
                    string.IsNullOrWhiteSpace(model.Symptoms)
                    ? DBNull.Value
                    : (object)model.Symptoms);

                cmd.Parameters.AddWithValue("@Medicines", model.Medicines);

                cmd.Parameters.AddWithValue("@Dosage", model.Dosage);

                cmd.Parameters.AddWithValue("@Instructions",
                    string.IsNullOrWhiteSpace(model.Instructions)
                    ? DBNull.Value
                    : (object)model.Instructions);

                cmd.Parameters.AddWithValue("@NextVisitDate",
                    model.NextVisitDate.HasValue
                    ? (object)model.NextVisitDate.Value
                    : DBNull.Value);

                cmd.Parameters.AddWithValue("@PrescriptionStatus",
                    string.IsNullOrWhiteSpace(model.PrescriptionStatus)
                    ? "Active"
                    : model.PrescriptionStatus);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {

                    string updateQuery = @"
UPDATE tbl_Appointment
SET UpdatedDate=@UpdatedDate
WHERE AppointmentId=@AppointmentId";

                    SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                    updateCmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                    updateCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                    updateCmd.ExecuteNonQuery();

                    TempData["Success"] = "Prescription added successfully.";

                    return RedirectToAction("ManagePrescriptions");
                }
            }

            TempData["Error"] = "Unable to save prescription.";

            return View(model);
        }
    
        [HttpGet]
        public IActionResult ViewPrescription(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE
p.PrescriptionId=@PrescriptionId
AND p.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = dr["Experience"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.Medicines = dr["Medicines"].ToString();

                    model.Dosage = dr["Dosage"].ToString();

                    model.Instructions = dr["Instructions"].ToString();

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.PrescriptionId == 0)
            {
                TempData["Error"] = "Prescription not found.";

                return RedirectToAction("ManagePrescriptions");
            }

            return View(model);
        }
        // ==========================================
        // GET : Edit Prescription
        // ==========================================
        [HttpGet]
        public IActionResult EditPrescription(long id)
        {
            // Admin Login Check
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE
p.PrescriptionId=@PrescriptionId
AND p.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = dr["Experience"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.Medicines = dr["Medicines"].ToString();

                    model.Dosage = dr["Dosage"].ToString();

                    model.Instructions = dr["Instructions"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.PrescriptionId == 0)
            {
                TempData["Error"] = "Prescription not found.";

                return RedirectToAction("ManagePrescriptions");
            }

            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem()
        {
            Text = "Active",
            Value = "Active"
        },

        new SelectListItem()
        {
            Text = "Completed",
            Value = "Completed"
        }
    };

            return View(model);
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPrescription(PrescriptionModel model)
        {
            // Admin Login Check
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // Remove Display Properties
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("CustomerName");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("DoctorName");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("Qualification");
            ModelState.Remove("Specialization");
            ModelState.Remove("Experience");
            ModelState.Remove("AppointmentDate");
            ModelState.Remove("AppointmentTime");
            ModelState.Remove("ClinicName");
            ModelState.Remove("ClinicAddress");
            ModelState.Remove("ClinicMobile");
            ModelState.Remove("ClinicEmail");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new List<SelectListItem>()
        {
            new SelectListItem(){ Text="Active", Value="Active"},
            new SelectListItem(){ Text="Completed", Value="Completed"}
        };

                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
UPDATE tbl_Prescription
SET

    Diagnosis=@Diagnosis,
    Symptoms=@Symptoms,
    Medicines=@Medicines,
    Dosage=@Dosage,
    Instructions=@Instructions,
    NextVisitDate=@NextVisitDate,
    PrescriptionStatus=@PrescriptionStatus,
    UpdatedDate=@UpdatedDate

WHERE
PrescriptionId=@PrescriptionId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Diagnosis", model.Diagnosis);

                cmd.Parameters.AddWithValue("@Symptoms",
                    string.IsNullOrWhiteSpace(model.Symptoms)
                    ? DBNull.Value
                    : (object)model.Symptoms);

                cmd.Parameters.AddWithValue("@Medicines", model.Medicines);

                cmd.Parameters.AddWithValue("@Dosage", model.Dosage);

                cmd.Parameters.AddWithValue("@Instructions",
                    string.IsNullOrWhiteSpace(model.Instructions)
                    ? DBNull.Value
                    : (object)model.Instructions);

                cmd.Parameters.AddWithValue("@NextVisitDate",
                    model.NextVisitDate.HasValue
                    ? (object)model.NextVisitDate.Value
                    : DBNull.Value);

                cmd.Parameters.AddWithValue("@PrescriptionStatus",
                    string.IsNullOrWhiteSpace(model.PrescriptionStatus)
                    ? "Active"
                    : model.PrescriptionStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                cmd.Parameters.AddWithValue("@PrescriptionId", model.PrescriptionId);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Prescription updated successfully.";

                    return RedirectToAction("ManagePrescriptions");
                }
                else
                {
                    TempData["Error"] = "Unable to update prescription.";
                }
            }

            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem(){ Text="Active", Value="Active"},
        new SelectListItem(){ Text="Completed", Value="Completed"}
    };

            return View(model);
        }
       
        [HttpGet]
        public IActionResult DownloadPrescriptionPDF(long id)
        {
           
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.Qualification,
    d.Specialization,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

p.PrescriptionId=@PrescriptionId
AND p.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.Medicines = dr["Medicines"].ToString();

                    model.Dosage = dr["Dosage"].ToString();

                    model.Instructions = dr["Instructions"].ToString();

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);
                }

                dr.Close();
            }

            if (model.PrescriptionId == 0)
            {
                TempData["Error"] = "Prescription not found.";

                return RedirectToAction("ManagePrescriptions");
            }

            byte[] pdf = Document.Create(container =>
            {
            container.Page(page =>
            {
            page.Margin(30);

            page.Size(PageSizes.A4);

            page.DefaultTextStyle(x => x.FontSize(11));


            page.Header()
                .Column(col =>
                {
                    col.Spacing(5);

                    col.Item().AlignCenter().Text("CLINIC MANAGEMENT SYSTEM")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item().AlignCenter().Text("Medical Prescription")
                        .FontSize(18)
                        .Bold();

                    col.Item().AlignCenter().Text("123, Health Street, Ahmedabad, Gujarat")
                        .FontSize(10);

                    col.Item().AlignCenter().Text("Phone : +91 9876543210 | Email : clinic@gmail.com")
                        .FontSize(10);

                    col.Item().PaddingTop(8);

                    col.Item().LineHorizontal(1);
                });


            page.Content()
                .PaddingVertical(15)
                .Column(column =>
                {
                column.Spacing(12);

               

                column.Item()
                    .AlignCenter()
                    .Text("PRESCRIPTION")
                    .Bold()
                    .FontSize(20)
                    .FontColor(Colors.Green.Darken2);

               

                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item().Text("Appointment Information")
                            .Bold()
                            .FontSize(14);

                        info.Item().Text($"Appointment No : {model.AppointmentNo}");

                        info.Item().Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");

                        info.Item().Text($"Appointment Time : {model.AppointmentTime}");
                    });


                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item().Text("Patient Information")
                            .Bold()
                            .FontSize(14);

                        info.Item().Text($"Patient Name : {model.CustomerName}");

                        info.Item().Text($"Mobile : {model.MobileNo}");

                        info.Item().Text($"Email : {model.Email}");
                    });


                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item().Text("Doctor Information")
                            .Bold()
                            .FontSize(14);

                        info.Item().Text($"Doctor : Dr. {model.DoctorName}");

                        info.Item().Text($"Department : {model.DepartmentName}");

                        info.Item().Text($"Qualification : {model.Qualification}");

                        info.Item().Text($"Specialization : {model.Specialization}");
                    });

                column.Item()
.Border(1)
.Padding(12)
.Column(info =>
{
    info.Spacing(10);

    info.Item().Text("Prescription Details")
        .Bold()
        .FontSize(15)
        .FontColor(Colors.Red.Darken2);

    
    info.Item().Text("Diagnosis")
        .Bold();

    info.Item().Text(model.Diagnosis ?? "-");

    info.Item().LineHorizontal(0.5f);

    info.Item().Text("Symptoms")
        .Bold();

    info.Item().Text(
        string.IsNullOrWhiteSpace(model.Symptoms)
        ? "-"
        : model.Symptoms);

    info.Item().LineHorizontal(0.5f);

  
    info.Item().Text("Medicines")
        .Bold();

    info.Item()
        .Background(Colors.Grey.Lighten4)
        .Padding(8)
        .Text(model.Medicines ?? "-");

    
    info.Item().PaddingTop(8);

    info.Item().Text("Dosage")
        .Bold();

    info.Item()
        .Background(Colors.Grey.Lighten4)
        .Padding(8)
        .Text(model.Dosage ?? "-");

  
    info.Item().PaddingTop(8);

    info.Item().Text("Doctor Instructions")
        .Bold();

    info.Item()
        .Background(Colors.Grey.Lighten4)
        .Padding(8)
        .Text(
            string.IsNullOrWhiteSpace(model.Instructions)
            ? "-"
            : model.Instructions);


    info.Item().PaddingTop(10);

    info.Item().Row(row =>
    {
        row.RelativeItem().Text(txt =>
        {
            txt.Span("Next Visit Date : ").Bold();

            txt.Span(
                model.NextVisitDate.HasValue
                ? model.NextVisitDate.Value.ToString("dd MMM yyyy")
                : "-");
        });

        row.RelativeItem().AlignRight().Text(txt =>
        {
            txt.Span("Status : ").Bold();

            txt.Span(model.PrescriptionStatus ?? "Active");
        });
    });
});


                column.Item().PaddingTop(25);

                column.Item().AlignRight()
                    .Column(signature =>
                    {
                        signature.Item().Text("----------------------------");

                        signature.Item()
                            .AlignCenter()
                            .Text("Doctor Signature")
                            .Bold();

                        signature.Item()
                            .AlignCenter()
                            .Text($"Dr. {model.DoctorName}");

                        signature.Item()
                            .AlignCenter()
                            .Text(model.Qualification ?? "");

                        signature.Item()
                            .AlignCenter()
                            .Text(model.Specialization ?? "");
                    });
                    

                    column.Item()
                        .PaddingTop(20)
                        .AlignCenter()
                        .Text("Get Well Soon!")
                        .Bold()
                        .FontSize(18)
                        .FontColor(Colors.Green.Darken2);

                    column.Item()
                        .AlignCenter()
                        .Text("Please follow the doctor's advice and complete the prescribed medicines.")
                        .FontSize(10);

                });


                page.Footer()
                    .PaddingTop(10)
                    .Column(col =>
                    {
                        col.Item().LineHorizontal(1);

                        col.Item().PaddingTop(5);

                        col.Item()
                            .AlignCenter()
                            .Text(txt =>
                            {
                                txt.Span("Generated On : ").Bold();

                                txt.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
                            });

                        col.Item()
                            .AlignCenter()
                            .Text("This is a computer generated prescription.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Page ");

                                text.CurrentPageNumber();

                                text.Span(" of ");

                                text.TotalPages();
                            });
                    });

            });

            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                "Prescription_" + model.AppointmentNo + ".pdf");
        }
    
        [HttpGet]
        public IActionResult DeletePrescription(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"
SELECT COUNT(*)
FROM tbl_Prescription
WHERE PrescriptionId=@PrescriptionId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@PrescriptionId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Prescription not found.";

                    return RedirectToAction("ManagePrescriptions");
                }

                

                string query = @"
UPDATE tbl_Prescription
SET

    IsDeleted=1,
    UpdatedDate=@UpdatedDate

WHERE
PrescriptionId=@PrescriptionId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Prescription deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete prescription.";
                }
            }

            return RedirectToAction("ManagePrescriptions");
        }
      
        [HttpGet]
        public IActionResult ManageBills(string search = "", string paymentStatus = "", string billStatus = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<BillingModel> list = new List<BillingModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

    b.*,

    a.AppointmentNo,

    c.FullName,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON b.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

b.IsDeleted = 0

AND
(
    @PaymentStatus = ''
    OR
    b.PaymentStatus = @PaymentStatus
)

AND
(
    @BillStatus = ''
    OR
    b.BillStatus = @BillStatus
)

AND
(
    b.BillNo LIKE @Search

    OR

    a.AppointmentNo LIKE @Search

    OR

    c.FullName LIKE @Search

    OR

    d.DoctorName LIKE @Search
)

ORDER BY b.BillDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus ?? "");
                cmd.Parameters.AddWithValue("@BillStatus", billStatus ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    BillingModel model = new BillingModel();

                    model.BillingId = Convert.ToInt64(dr["BillingId"]);

                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);

                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);

                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);

                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.BillStatus = dr["BillStatus"].ToString();

                    model.BillDate = Convert.ToDateTime(dr["BillDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.BillStatus = billStatus;
            ViewBag.TotalBills = list.Count;

            return View(list);
        }
        
        [HttpGet]
        public IActionResult GenerateBill(long appointmentId)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            BillingModel model = new BillingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string billCheck = @"SELECT COUNT(*)
                             FROM tbl_Billing
                             WHERE AppointmentId=@AppointmentId
                             AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(billCheck, con);
                checkCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    TempData["Error"] = "Bill already generated.";

                    return RedirectToAction("ManageBills");
                }


                string paymentQuery = @"SELECT PaymentStatus
                                FROM tbl_Payment
                                WHERE AppointmentId=@AppointmentId";

                SqlCommand paymentCmd = new SqlCommand(paymentQuery, con);
                paymentCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                object payment = paymentCmd.ExecuteScalar();

                if (payment == null || payment.ToString() != "Paid")
                {
                    TempData["Error"] = "Bill can be generated only after payment is completed.";

                    return RedirectToAction("ManageAppointments");
                }

          

                string query = @"

SELECT

a.AppointmentId,
a.AppointmentNo,
a.AppointmentDate,
a.AppointmentTime,
a.ConsultationFee,

c.CustomerId,
c.FullName,
c.MobileNo,
c.Email,

d.DoctorId,
d.DoctorName,

dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
ON a.CustomerId=c.CustomerId

INNER JOIN tbl_Doctor d
ON a.DoctorId=d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId=dep.DepartmentId

WHERE
a.AppointmentId=@AppointmentId
AND a.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.BillDate = DateTime.Now;

                    model.PaymentStatus = "Paid";

                    model.BillStatus = "Generated";
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";

                return RedirectToAction("ManageAppointments");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateBill(BillingModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // Remove Validation
            ModelState.Remove("BillNo");
            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("BillDate");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string billNo = "BILL" + DateTime.Now.ToString("yyyyMMddHHmmss");


                decimal subTotal =
                    model.ConsultationFee +
                    model.MedicineCharge +
                    model.LabCharge +
                    model.OtherCharge;

                decimal amountAfterDiscount = subTotal - model.Discount;

                if (amountAfterDiscount < 0)
                    amountAfterDiscount = 0;

                model.GSTAmount = (amountAfterDiscount * model.GSTPercentage) / 100;

                model.TotalAmount = amountAfterDiscount + model.GSTAmount;

                string query = @"
INSERT INTO tbl_Billing
(
    BillNo,
    AppointmentId,
    CustomerId,
    DoctorId,

    ConsultationFee,
    MedicineCharge,
    LabCharge,
    OtherCharge,

    Discount,

    GSTPercentage,
    GSTAmount,

    TotalAmount,

    PaymentStatus,
    BillStatus,

    BillDate,

    Remarks,

    IsActive,
    IsDeleted,

    CreatedDate
)

VALUES
(
    @BillNo,
    @AppointmentId,
    @CustomerId,
    @DoctorId,

    @ConsultationFee,
    @MedicineCharge,
    @LabCharge,
    @OtherCharge,

    @Discount,

    @GSTPercentage,
    @GSTAmount,

    @TotalAmount,

    @PaymentStatus,
    @BillStatus,

    @BillDate,

    @Remarks,

    1,
    0,

    @CreatedDate
)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BillNo", billNo);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                cmd.Parameters.AddWithValue("@MedicineCharge", model.MedicineCharge);
                cmd.Parameters.AddWithValue("@LabCharge", model.LabCharge);
                cmd.Parameters.AddWithValue("@OtherCharge", model.OtherCharge);

                cmd.Parameters.AddWithValue("@Discount", model.Discount);

                cmd.Parameters.AddWithValue("@GSTPercentage", model.GSTPercentage);
                cmd.Parameters.AddWithValue("@GSTAmount", model.GSTAmount);

                cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);

                cmd.Parameters.AddWithValue("@PaymentStatus",
                    string.IsNullOrWhiteSpace(model.PaymentStatus)
                    ? "Paid"
                    : model.PaymentStatus);

                cmd.Parameters.AddWithValue("@BillStatus",
                    string.IsNullOrWhiteSpace(model.BillStatus)
                    ? "Generated"
                    : model.BillStatus);

                cmd.Parameters.AddWithValue("@BillDate", DateTime.Now);

                if (string.IsNullOrWhiteSpace(model.Remarks))
                    cmd.Parameters.AddWithValue("@Remarks", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Bill generated successfully.";

                    return RedirectToAction("ManageBills");
                }
                else
                {
                    TempData["Error"] = "Unable to generate bill.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ViewBill(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            BillingModel model = new BillingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

b.*,

a.AppointmentNo,
a.AppointmentDate,
a.AppointmentTime,

c.FullName,
c.MobileNo,
c.Email,

d.DoctorName,
d.DoctorImage,

dep.DepartmentName

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON b.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

b.BillingId=@BillingId
AND b.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BillingId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.BillingId = Convert.ToInt64(dr["BillingId"]);

                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);

                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);

                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);

                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.BillStatus = dr["BillStatus"].ToString();

                    if (dr["BillDate"] != DBNull.Value)
                        model.BillDate = Convert.ToDateTime(dr["BillDate"]);

                    model.Remarks = dr["Remarks"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.BillingId == 0)
            {
                TempData["Error"] = "Bill not found.";

                return RedirectToAction("ManageBills");
            }

            return View(model);
        }
 
        [HttpGet]
        public IActionResult DownloadBillPDF(long id)
        {
          
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            BillingModel model = new BillingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

b.*,

a.AppointmentNo,
a.AppointmentDate,
a.AppointmentTime,

c.FullName,
c.MobileNo,
c.Email,

d.DoctorName,
d.DoctorImage,

dep.DepartmentName

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON b.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

b.BillingId=@BillingId
AND b.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BillingId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.BillingId = Convert.ToInt64(dr["BillingId"]);

                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);

                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);

                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);

                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.BillStatus = dr["BillStatus"].ToString();

                    model.Remarks = dr["Remarks"].ToString();

                    if (dr["BillDate"] != DBNull.Value)
                        model.BillDate = Convert.ToDateTime(dr["BillDate"]);
                }

                dr.Close();
            }

            if (model.BillingId == 0)
            {
                TempData["Error"] = "Bill not found.";

                return RedirectToAction("ManageBills");
            }

            byte[] pdf = Document.Create(container =>
            {
            container.Page(page =>
            {
            page.Size(PageSizes.A4);

            page.Margin(30);

            page.DefaultTextStyle(x => x.FontSize(11));

        

            page.Header()
                .Column(col =>
                {
                    col.Spacing(5);

                    col.Item()
                        .AlignCenter()
                        .Text("CLINIC MANAGEMENT SYSTEM")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item()
                        .AlignCenter()
                        .Text("MEDICAL BILL / TAX INVOICE")
                        .FontSize(18)
                        .Bold();

                    col.Item()
                        .AlignCenter()
                        .Text("123 Health Street, Ahmedabad, Gujarat")
                        .FontSize(10);

                    col.Item()
                        .AlignCenter()
                        .Text("Phone : +91 9876543210 | Email : clinic@gmail.com")
                        .FontSize(10);

                    col.Item()
                        .PaddingTop(8);

                    col.Item()
                        .LineHorizontal(1);

                });


            page.Content()
                .PaddingVertical(15)
                .Column(column =>
                {
                column.Spacing(15);

                column.Item()
                    .AlignCenter()
                    .Text("MEDICAL BILL")
                    .Bold()
                    .FontSize(20)
                    .FontColor(Colors.Red.Darken2);


                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Bill Information")
                            .Bold()
                            .FontSize(14);

                        info.Item()
                            .Text($"Bill No : {model.BillNo}");

                        info.Item()
                            .Text($"Bill Date : {model.BillDate:dd MMM yyyy}");

                        info.Item()
                            .Text($"Payment Status : {model.PaymentStatus}");

                        info.Item()
                            .Text($"Bill Status : {model.BillStatus}");
                    });


                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Patient Information")
                            .Bold()
                            .FontSize(14);

                        info.Item()
                            .Text($"Patient Name : {model.CustomerName}");

                        info.Item()
                            .Text($"Mobile Number : {model.MobileNo}");

                        info.Item()
                            .Text($"Email Address : {model.Email}");
                    });


                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Doctor Information")
                            .Bold()
                            .FontSize(14);

                        info.Item()
                            .Text($"Doctor : Dr. {model.DoctorName}");

                        info.Item()
                            .Text($"Department : {model.DepartmentName}");
                    });

       

                column.Item()
                    .Border(1)
                    .Padding(10)
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Appointment Information")
                            .Bold()
                            .FontSize(14);

                        info.Item()
                            .Text($"Appointment No : {model.AppointmentNo}");

                        info.Item()
                            .Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");

                        info.Item()
                            .Text($"Appointment Time : {model.AppointmentTime}");
                    });

                    column.Item()
    .Border(1)
    .Padding(10)
    .Column(info =>
    {
        info.Item()
            .Text("Charges Details")
            .Bold()
            .FontSize(15)
            .FontColor(Colors.Green.Darken2);

        info.Item().PaddingTop(10);

        info.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

          

            table.Header(header =>
            {
                header.Cell()
                    .Background(Colors.Grey.Lighten2)
                    .Padding(8)
                    .Text("Particular")
                    .Bold();

                header.Cell()
                    .Background(Colors.Grey.Lighten2)
                    .Padding(8)
                    .AlignRight()
                    .Text("Amount")
                    .Bold();
            });

            

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Consultation Fee");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"₹ {model.ConsultationFee:0.00}");

           

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Medicine Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"₹ {model.MedicineCharge:0.00}");

          

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Lab Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"₹ {model.LabCharge:0.00}");

           

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Other Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"₹ {model.OtherCharge:0.00}");

         

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Discount");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"- ₹ {model.Discount:0.00}");

            

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text($"GST ({model.GSTPercentage:0.##}%)");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"₹ {model.GSTAmount:0.00}");
        });

       

        info.Item()
            .PaddingTop(15);

        info.Item()
            .Background(Colors.Green.Lighten4)
            .Border(1)
            .BorderColor(Colors.Green.Darken1)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Grand Total")
                    .Bold()
                    .FontSize(16);

                row.RelativeItem()
                    .AlignRight()
                    .Text($"₹ {model.TotalAmount:0.00}")
                    .Bold()
                    .FontSize(18)
                    .FontColor(Colors.Green.Darken3);
            });

      

        if (!string.IsNullOrWhiteSpace(model.Remarks))
        {
            info.Item()
                .PaddingTop(15);

            info.Item()
                .Text("Remarks")
                .Bold()
                .FontSize(13);

            info.Item()
                .Border(1)
                .Padding(8)
                .Text(model.Remarks);
        }
    });


                    column.Item()
                        .PaddingTop(20)
                        .Border(1)
                        .BorderColor(Colors.Blue.Lighten2)
                        .Background(Colors.Blue.Lighten5)
                        .Padding(10)
                        .Column(note =>
                        {
                            note.Item()
                                .Text("Important Note")
                                .Bold()
                                .FontSize(14)
                                .FontColor(Colors.Blue.Darken2);

                            note.Item()
                                .Text("• This is a computer-generated medical bill.");

                            note.Item()
                                .Text("• Please keep this invoice for future reference.");

                            note.Item()
                                .Text("• Contact the clinic for any billing queries.");

                            note.Item()
                                .Text("• Thank you for choosing our clinic.");
                        });
                  

                    column.Item()
                        .PaddingTop(30);

                    column.Item()
                        .AlignRight()
                        .Column(sign =>
                        {
                            sign.Item()
                                .Text("--------------------------------");

                            sign.Item()
                                .AlignCenter()
                                .Text("Authorized Signature")
                                .Bold();

                            sign.Item()
                                .AlignCenter()
                                .Text("Clinic Administrator");

                            sign.Item()
                                .AlignCenter()
                                .Text("Clinic Management System");
                        });


                    column.Item()
                        .PaddingTop(20)
                        .AlignCenter()
                        .Text("Thank You For Visiting Our Clinic")
                        .Bold()
                        .FontSize(18)
                        .FontColor(Colors.Green.Darken2);

                    column.Item()
                        .AlignCenter()
                        .Text("We wish you a healthy and happy life.")
                        .FontSize(10);

                });


                page.Footer()
                    .PaddingTop(10)
                    .Column(col =>
                    {
                        col.Item().LineHorizontal(1);

                        col.Item()
                            .PaddingTop(5)
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Generated On : ").Bold();

                                text.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
                            });

                        col.Item()
                            .AlignCenter()
                            .Text("This is a computer generated invoice.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Page ");

                                text.CurrentPageNumber();

                                text.Span(" of ");

                                text.TotalPages();
                            });

                    });

            });

            }).GeneratePdf();

            string fileName = $"Bill_{model.BillNo}.pdf";

            return File(
                pdf,
                "application/pdf",
                fileName);
        }
      
        [HttpGet]
        public IActionResult DeleteBill(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"
        SELECT COUNT(*)
        FROM tbl_Billing
        WHERE BillingId=@BillingId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@BillingId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Bill not found.";

                    return RedirectToAction("ManageBills");
                }


                string deleteQuery = @"

        UPDATE tbl_Billing

        SET

            IsDeleted = 1,
            UpdatedDate = @UpdatedDate

        WHERE

            BillingId = @BillingId";

                SqlCommand cmd = new SqlCommand(deleteQuery, con);

                cmd.Parameters.AddWithValue("@BillingId", id);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Bill deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete bill.";
                }
            }

            return RedirectToAction("ManageBills");
        }

        [HttpGet]
        public IActionResult ManageGallery(string search = "", string category = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<GalleryModel> list = new List<GalleryModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT *

FROM tbl_Gallery

WHERE

IsDeleted = 0

AND
(
    @Search = ''
    OR
    GalleryTitle LIKE @SearchText
)

AND
(
    @Category = ''
    OR
    GalleryCategory = @Category
)

ORDER BY

DisplayOrder ASC,
GalleryId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");
                cmd.Parameters.AddWithValue("@Category", category ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    GalleryModel model = new GalleryModel();

                    model.GalleryId = Convert.ToInt64(dr["GalleryId"]);

                    model.GalleryTitle = dr["GalleryTitle"].ToString();

                    model.GalleryCategory = dr["GalleryCategory"].ToString();

                    model.GalleryImage = dr["GalleryImage"].ToString();

                    model.Description = dr["Description"] == DBNull.Value
                                        ? ""
                                        : dr["Description"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Category = category;

            return View(list);
        }

        [HttpGet]
        public IActionResult AddGallery()
        {
            
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            GalleryModel model = new GalleryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

               
                string query = @"
        SELECT ISNULL(MAX(DisplayOrder),0) + 1
        FROM tbl_Gallery
        WHERE IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                model.DisplayOrder = Convert.ToInt32(cmd.ExecuteScalar());
            }

          
            model.IsActive = true;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddGallery(GalleryModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("GalleryImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

               

                string fileName = "";

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string folderPath = Path.Combine(env.WebRootPath, "GalleryImages");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);

                    string filePath = Path.Combine(folderPath, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.ImageFile.CopyTo(stream);
                    }
                }

               

                string query = @"

INSERT INTO tbl_Gallery
(
    GalleryTitle,
    GalleryCategory,
    GalleryImage,
    Description,
    DisplayOrder,
    IsActive,
    IsDeleted,
    CreatedDate
)

VALUES
(
    @GalleryTitle,
    @GalleryCategory,
    @GalleryImage,
    @Description,
    @DisplayOrder,
    @IsActive,
    0,
    @CreatedDate
)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryTitle", model.GalleryTitle);

                cmd.Parameters.AddWithValue("@GalleryCategory", model.GalleryCategory);

                cmd.Parameters.AddWithValue("@GalleryImage", fileName);

                cmd.Parameters.AddWithValue("@Description",
                    string.IsNullOrWhiteSpace(model.Description)
                    ? (object)DBNull.Value
                    : model.Description);

                cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Gallery added successfully.";

                    return RedirectToAction("ManageGallery");
                }
                else
                {
                    TempData["Error"] = "Unable to add gallery.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ViewGallery(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            GalleryModel model = new GalleryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT *

FROM tbl_Gallery

WHERE

GalleryId = @GalleryId
AND IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.GalleryId = Convert.ToInt64(dr["GalleryId"]);

                    model.GalleryTitle = dr["GalleryTitle"].ToString();

                    model.GalleryCategory = dr["GalleryCategory"].ToString();

                    model.GalleryImage = dr["GalleryImage"].ToString();

                    model.Description = dr["Description"] == DBNull.Value
                        ? ""
                        : dr["Description"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    TempData["Error"] = "Gallery record not found.";

                    return RedirectToAction("ManageGallery");
                }

                dr.Close();
            }

            return View(model);
        }
        
        [HttpGet]
        public IActionResult EditGallery(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            GalleryModel model = new GalleryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT *

FROM tbl_Gallery

WHERE

GalleryId = @GalleryId
AND IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.GalleryId = Convert.ToInt64(dr["GalleryId"]);

                    model.GalleryTitle = dr["GalleryTitle"].ToString();

                    model.GalleryCategory = dr["GalleryCategory"].ToString();

                    model.GalleryImage = dr["GalleryImage"].ToString();

                    model.Description = dr["Description"] == DBNull.Value
                        ? ""
                        : dr["Description"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Gallery record not found.";

                    return RedirectToAction("ManageGallery");
                }

                dr.Close();
            }

            return View(model);
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditGallery(GalleryModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string oldImage = "";

                string getImageQuery = @"SELECT GalleryImage
                                 FROM tbl_Gallery
                                 WHERE GalleryId=@GalleryId";

                SqlCommand imgCmd = new SqlCommand(getImageQuery, con);

                imgCmd.Parameters.AddWithValue("@GalleryId", model.GalleryId);

                object result = imgCmd.ExecuteScalar();

                if (result != null)
                {
                    oldImage = result.ToString();
                }

                string fileName = oldImage;


                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string folderPath = Path.Combine(env.WebRootPath, "GalleryImages");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    fileName = Guid.NewGuid().ToString()
                             + Path.GetExtension(model.ImageFile.FileName);

                    string filePath = Path.Combine(folderPath, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.ImageFile.CopyTo(stream);
                    }

                    
                    if (!string.IsNullOrEmpty(oldImage))
                    {
                        string oldFile = Path.Combine(folderPath, oldImage);

                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }
                    }
                }


                string query = @"

UPDATE tbl_Gallery

SET

GalleryTitle=@GalleryTitle,
GalleryCategory=@GalleryCategory,
GalleryImage=@GalleryImage,
Description=@Description,
DisplayOrder=@DisplayOrder,
IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

GalleryId=@GalleryId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryId", model.GalleryId);

                cmd.Parameters.AddWithValue("@GalleryTitle", model.GalleryTitle);

                cmd.Parameters.AddWithValue("@GalleryCategory", model.GalleryCategory);

                cmd.Parameters.AddWithValue("@GalleryImage", fileName);

                cmd.Parameters.AddWithValue("@Description",
                    string.IsNullOrWhiteSpace(model.Description)
                    ? (object)DBNull.Value
                    : model.Description);

                cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = "Gallery updated successfully.";

                    return RedirectToAction("ManageGallery");
                }
                else
                {
                    TempData["Error"] = "Unable to update gallery.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangeGalleryStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string getStatusQuery = @"
        SELECT IsActive
        FROM tbl_Gallery
        WHERE GalleryId = @GalleryId
        AND IsDeleted = 0";

                SqlCommand getCmd = new SqlCommand(getStatusQuery, con);
                getCmd.Parameters.AddWithValue("@GalleryId", id);

                object result = getCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Gallery record not found.";
                    return RedirectToAction("ManageGallery");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;

               
                string updateQuery = @"

UPDATE tbl_Gallery

SET

IsActive = @IsActive,
UpdatedDate = @UpdatedDate

WHERE

GalleryId = @GalleryId";

                SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@GalleryId", id);
                updateCmd.Parameters.AddWithValue("@IsActive", newStatus);
                updateCmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int rows = updateCmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Gallery activated successfully."
                        : "Gallery deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to change gallery status.";
                }
            }

            return RedirectToAction("ManageGallery");
        }

        
        [HttpGet]
        public IActionResult DeleteGallery(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

              
                string checkQuery = @"
            SELECT GalleryImage
            FROM tbl_Gallery
            WHERE GalleryId = @GalleryId
            AND IsDeleted = 0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@GalleryId", id);

                object obj = checkCmd.ExecuteScalar();

                if (obj == null)
                {
                    TempData["Error"] = "Gallery not found.";

                    return RedirectToAction("ManageGallery");
                }

              
                string query = @"

UPDATE tbl_Gallery

SET

    IsDeleted = 1,
    UpdatedDate = @UpdatedDate

WHERE

    GalleryId = @GalleryId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryId", id);
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Gallery deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete gallery.";
                }
            }

            return RedirectToAction("ManageGallery");
        }
      
        [HttpGet]
        public IActionResult ManageFeedback(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<FeedbackModel> list = new List<FeedbackModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.IsDeleted,
F.CreatedDate,
F.UpdatedDate,

C.FullName AS CustomerName,

D.DoctorName,

A.AppointmentNo,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Customer C
ON F.CustomerId = C.CustomerId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

WHERE

F.IsDeleted = 0

AND
(
    @Search = ''

    OR C.FullName LIKE @SearchText

    OR D.DoctorName LIKE @SearchText

    OR A.AppointmentNo LIKE @SearchText

    OR F.Subject LIKE @SearchText
)

ORDER BY

F.FeedbackId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    FeedbackModel model = new FeedbackModel();

                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
       
        [HttpGet]
        public IActionResult ViewFeedback(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            FeedbackModel model = new FeedbackModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.CreatedDate,
F.UpdatedDate,

C.FullName AS CustomerName,

A.AppointmentNo,
A.AppointmentDate,

D.DoctorName,
D.Specialization,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Customer C
ON F.CustomerId = C.CustomerId

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

F.FeedbackId = @FeedbackId
AND F.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorSpecialization = dr["Specialization"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("ManageFeedback");
                }

                dr.Close();
            }

            return View(model);
        }
      
        [HttpGet]
        public IActionResult ReplyFeedback(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            FeedbackModel model = new FeedbackModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.CreatedDate,
F.UpdatedDate,

C.FullName AS CustomerName,

A.AppointmentNo,
A.AppointmentDate,

D.DoctorName,
D.Specialization,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Customer C
ON F.CustomerId = C.CustomerId

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

F.FeedbackId=@FeedbackId
AND F.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorSpecialization = dr["Specialization"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("ManageFeedback");
                }

                dr.Close();
            }

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReplyFeedback(FeedbackModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("DoctorSpecialization");
            ModelState.Remove("CustomerImage");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("FeedbackMessage");
            ModelState.Remove("Subject");

            if (!ModelState.IsValid)
            {
                return ReplyFeedback(model.FeedbackId);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_Feedback

WHERE

FeedbackId=@FeedbackId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@FeedbackId", model.FeedbackId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("ManageFeedback");
                }


                string query = @"

UPDATE tbl_Feedback

SET

ReplyMessage=@ReplyMessage,
IsApproved=@IsApproved,
UpdatedDate=@UpdatedDate

WHERE

FeedbackId=@FeedbackId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", model.FeedbackId);

                cmd.Parameters.AddWithValue("@ReplyMessage", model.ReplyMessage);

                cmd.Parameters.AddWithValue("@IsApproved", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Reply submitted successfully.";

                    return RedirectToAction("ManageFeedback");
                }
                else
                {
                    TempData["Error"] = "Unable to submit reply.";
                }
            }

            return RedirectToAction("ManageFeedback");
        }
       
        [HttpGet]
        public IActionResult ChangeFeedbackStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsApproved

FROM tbl_Feedback

WHERE

FeedbackId=@FeedbackId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@FeedbackId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("ManageFeedback");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;


                string query = @"

UPDATE tbl_Feedback

SET

IsApproved=@IsApproved,
UpdatedDate=@UpdatedDate

WHERE

FeedbackId=@FeedbackId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);

                cmd.Parameters.AddWithValue("@IsApproved", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Feedback approved successfully."
                        : "Feedback marked as pending successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update feedback status.";
                }
            }

            return RedirectToAction("ManageFeedback");
        }
        
        [HttpGet]
        public IActionResult DeleteFeedback(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_Feedback

WHERE

FeedbackId=@FeedbackId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@FeedbackId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("ManageFeedback");
                }


                string query = @"

UPDATE tbl_Feedback

SET

IsDeleted=@IsDeleted,
UpdatedDate=@UpdatedDate

WHERE

FeedbackId=@FeedbackId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);

                cmd.Parameters.AddWithValue("@IsDeleted", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Feedback deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete feedback.";
                }
            }

            return RedirectToAction("ManageFeedback");
        }
      
        [HttpGet]
        public IActionResult ManageContactInquiry(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<ContactInquiryModel> list = new List<ContactInquiryModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

InquiryId,
CustomerId,
FullName,
Email,
MobileNo,
Subject,
Message,
ReplyMessage,
IsReplied,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_ContactInquiry

WHERE

IsDeleted = 0

AND
(
    @Search = ''

    OR FullName LIKE @SearchText

    OR Email LIKE @SearchText

    OR Subject LIKE @SearchText

    OR MobileNo LIKE @SearchText
)

ORDER BY InquiryId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ContactInquiryModel model = new ContactInquiryModel();

                    model.InquiryId = Convert.ToInt64(dr["InquiryId"]);

                    model.CustomerId = dr["CustomerId"] == DBNull.Value
                        ? null
                        : Convert.ToInt64(dr["CustomerId"]);

                    model.FullName = dr["FullName"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Subject = dr["Subject"].ToString();

                    model.Message = dr["Message"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsReplied = Convert.ToBoolean(dr["IsReplied"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
 
        [HttpGet]
        public IActionResult ViewContactInquiry(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ContactInquiryModel model = new ContactInquiryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

InquiryId,
CustomerId,
FullName,
Email,
MobileNo,
Subject,
Message,
ReplyMessage,
IsReplied,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_ContactInquiry

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.InquiryId = Convert.ToInt64(dr["InquiryId"]);

                    model.CustomerId = dr["CustomerId"] == DBNull.Value
                        ? null
                        : Convert.ToInt64(dr["CustomerId"]);

                    model.FullName = dr["FullName"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Subject = dr["Subject"].ToString();

                    model.Message = dr["Message"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsReplied = Convert.ToBoolean(dr["IsReplied"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("ManageContactInquiry");
                }

                dr.Close();
            }

            return View(model);
        }
    
        [HttpGet]
        public IActionResult ReplyContactInquiry(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ContactInquiryModel model = new ContactInquiryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

InquiryId,
CustomerId,
FullName,
Email,
MobileNo,
Subject,
Message,
ReplyMessage,
IsReplied,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_ContactInquiry

WHERE

InquiryId = @InquiryId
AND IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.InquiryId = Convert.ToInt64(dr["InquiryId"]);

                    model.CustomerId = dr["CustomerId"] == DBNull.Value
                                        ? null
                                        : Convert.ToInt64(dr["CustomerId"]);

                    model.FullName = dr["FullName"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Subject = dr["Subject"].ToString();

                    model.Message = dr["Message"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                                            ? ""
                                            : dr["ReplyMessage"].ToString();

                    model.IsReplied = Convert.ToBoolean(dr["IsReplied"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("ManageContactInquiry");
                }

                dr.Close();
            }

            return View(model);
        }
   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReplyContactInquiry(ContactInquiryModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("FullName");
            ModelState.Remove("Email");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Subject");
            ModelState.Remove("Message");
            ModelState.Remove("CustomerName");
            ModelState.Remove("CustomerImage");
            ModelState.Remove("StatusText");

            if (!ModelState.IsValid)
            {
                return ReplyContactInquiry(model.InquiryId);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_ContactInquiry

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@InquiryId", model.InquiryId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("ManageContactInquiry");
                }


                string query = @"

UPDATE tbl_ContactInquiry

SET

ReplyMessage=@ReplyMessage,
IsReplied=@IsReplied,
UpdatedDate=@UpdatedDate

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", model.InquiryId);

                cmd.Parameters.AddWithValue("@ReplyMessage", model.ReplyMessage);

                cmd.Parameters.AddWithValue("@IsReplied", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Reply sent successfully.";

                    return RedirectToAction("ManageContactInquiry");
                }
                else
                {
                    TempData["Error"] = "Unable to send reply.";
                }
            }

            return RedirectToAction("ManageContactInquiry");
        }
      
        [HttpGet]
        public IActionResult ChangeInquiryStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsReplied

FROM tbl_ContactInquiry

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@InquiryId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("ManageContactInquiry");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;


                string query = @"

UPDATE tbl_ContactInquiry

SET

IsReplied=@IsReplied,
UpdatedDate=@UpdatedDate

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", id);

                cmd.Parameters.AddWithValue("@IsReplied", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Inquiry marked as replied successfully."
                        : "Inquiry marked as pending successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update inquiry status.";
                }
            }

            return RedirectToAction("ManageContactInquiry");
        }
       
        [HttpGet]
        public IActionResult DeleteContactInquiry(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_ContactInquiry

WHERE

InquiryId=@InquiryId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@InquiryId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("ManageContactInquiry");
                }


                string query = @"

UPDATE tbl_ContactInquiry

SET

IsDeleted=@IsDeleted,
UpdatedDate=@UpdatedDate

WHERE

InquiryId=@InquiryId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", id);

                cmd.Parameters.AddWithValue("@IsDeleted", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Contact inquiry deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete contact inquiry.";
                }
            }

            return RedirectToAction("ManageContactInquiry");
        }
       
        [HttpGet]
        public IActionResult ManageHealthTips(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<HealthTipModel> list = new List<HealthTipModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HealthTipId,
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HealthTip

WHERE

IsDeleted = 0

AND
(
    @Search = ''

    OR Title LIKE @SearchText

    OR Category LIKE @SearchText

    OR AuthorName LIKE @SearchText
)

ORDER BY

DisplayOrder ASC,
HealthTipId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    HealthTipModel model = new HealthTipModel();

                    model.HealthTipId = Convert.ToInt64(dr["HealthTipId"]);

                    model.Category = dr["Category"].ToString();

                    model.Title = dr["Title"].ToString();

                    model.ShortDescription = dr["ShortDescription"].ToString();

                    model.Description = dr["Description"].ToString();

                    model.TipImage = dr["TipImage"] == DBNull.Value
                                        ? ""
                                        : dr["TipImage"].ToString();

                    model.AuthorName = dr["AuthorName"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
       
        [HttpGet]
        public IActionResult AddHealthTip()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HealthTipModel model = new HealthTipModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string query = @"

SELECT ISNULL(MAX(DisplayOrder),0)+1

FROM tbl_HealthTip

WHERE IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                model.DisplayOrder = Convert.ToInt32(cmd.ExecuteScalar());

                model.IsActive = true;
                model.IsFeatured = false;
            }

            return View(model);
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddHealthTip(HealthTipModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("TipImage");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("StatusText");
            ModelState.Remove("FeaturedText");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string fileName = "";

                if (model.TipImageFile != null)
                {
                    string folderPath = Path.Combine(env.WebRootPath, "HealthTips");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(model.TipImageFile.FileName);

                    string filePath = Path.Combine(folderPath, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.TipImageFile.CopyTo(stream);
                    }
                }


                string query = @"

INSERT INTO tbl_HealthTip
(
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
IsActive,
IsDeleted,
CreatedDate
)

VALUES
(
@Category,
@Title,
@ShortDescription,
@Description,
@TipImage,
@AuthorName,
@DisplayOrder,
@IsFeatured,
@IsActive,
@IsDeleted,
@CreatedDate
)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Category", model.Category);

                cmd.Parameters.AddWithValue("@Title", model.Title);

                cmd.Parameters.AddWithValue("@ShortDescription", model.ShortDescription);

                cmd.Parameters.AddWithValue("@Description", model.Description);

                if (string.IsNullOrEmpty(fileName))
                {
                    cmd.Parameters.AddWithValue("@TipImage", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@TipImage", fileName);
                }

                cmd.Parameters.AddWithValue("@AuthorName", model.AuthorName);

                cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Health Tip added successfully.";

                    return RedirectToAction("ManageHealthTips");
                }
                else
                {
                    TempData["Error"] = "Unable to add Health Tip.";
                }
            }

            return View(model);
        }
     
        [HttpGet]
        public IActionResult EditHealthTip(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HealthTipModel model = new HealthTipModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HealthTipId,
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.HealthTipId = Convert.ToInt64(dr["HealthTipId"]);

                    model.Category = dr["Category"].ToString();

                    model.Title = dr["Title"].ToString();

                    model.ShortDescription = dr["ShortDescription"].ToString();

                    model.Description = dr["Description"].ToString();

                    model.TipImage = dr["TipImage"] == DBNull.Value
                                        ? ""
                                        : dr["TipImage"].ToString();

                    model.AuthorName = dr["AuthorName"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }

                dr.Close();
            }

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditHealthTip(HealthTipModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("TipImage");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("StatusText");
            ModelState.Remove("FeaturedText");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string oldImage = "";

                string getQuery = @"

SELECT TipImage

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand getCmd = new SqlCommand(getQuery, con);

                getCmd.Parameters.AddWithValue("@HealthTipId", model.HealthTipId);

                object img = getCmd.ExecuteScalar();

                if (img == null)
                {
                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }

                if (img != DBNull.Value)
                {
                    oldImage = img.ToString();
                }


                string fileName = oldImage;

                if (model.TipImageFile != null)
                {
                    string folderPath = Path.Combine(env.WebRootPath, "HealthTips");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    if (!string.IsNullOrEmpty(oldImage))
                    {
                        string oldFilePath = Path.Combine(folderPath, oldImage);

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                   
                    fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(model.TipImageFile.FileName);

                    string filePath = Path.Combine(folderPath, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.TipImageFile.CopyTo(stream);
                    }
                }


                string updateQuery = @"

UPDATE tbl_HealthTip

SET

Category=@Category,
Title=@Title,
ShortDescription=@ShortDescription,
Description=@Description,
TipImage=@TipImage,
AuthorName=@AuthorName,
DisplayOrder=@DisplayOrder,
IsFeatured=@IsFeatured,
IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@HealthTipId", model.HealthTipId);

                cmd.Parameters.AddWithValue("@Category", model.Category);

                cmd.Parameters.AddWithValue("@Title", model.Title);

                cmd.Parameters.AddWithValue("@ShortDescription", model.ShortDescription);

                cmd.Parameters.AddWithValue("@Description", model.Description);

                if (string.IsNullOrEmpty(fileName))
                {
                    cmd.Parameters.AddWithValue("@TipImage", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@TipImage", fileName);
                }

                cmd.Parameters.AddWithValue("@AuthorName", model.AuthorName);

                cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Health Tip updated successfully.";

                    return RedirectToAction("ManageHealthTips");
                }
                else
                {
                    TempData["Error"] = "Unable to update Health Tip.";
                }
            }

            return View(model);
        }
      
        [HttpGet]
        public IActionResult ViewHealthTip(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HealthTipModel model = new HealthTipModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HealthTipId,
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.HealthTipId = Convert.ToInt64(dr["HealthTipId"]);

                    model.Category = dr["Category"].ToString();

                    model.Title = dr["Title"].ToString();

                    model.ShortDescription = dr["ShortDescription"].ToString();

                    model.Description = dr["Description"].ToString();

                    model.TipImage = dr["TipImage"] == DBNull.Value
                                        ? ""
                                        : dr["TipImage"].ToString();

                    model.AuthorName = dr["AuthorName"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }

                dr.Close();
            }

            return View(model);
        }
  
        [HttpGet]
        public IActionResult ChangeHealthTipStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsActive

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HealthTipId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;


                string updateQuery = @"

UPDATE tbl_HealthTip

SET

IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                cmd.Parameters.AddWithValue("@IsActive", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Health Tip activated successfully."
                        : "Health Tip deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update Health Tip status.";
                }
            }

            return RedirectToAction("ManageHealthTips");
        }
       
        [HttpGet]
        public IActionResult ChangeFeaturedStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsFeatured

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HealthTipId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;


                string updateQuery = @"

UPDATE tbl_HealthTip

SET

IsFeatured=@IsFeatured,
UpdatedDate=@UpdatedDate

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                cmd.Parameters.AddWithValue("@IsFeatured", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Health Tip marked as Featured successfully."
                        : "Health Tip removed from Featured successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update Featured status.";
                }
            }

            return RedirectToAction("ManageHealthTips");
        }

        [HttpGet]
        public IActionResult DeleteHealthTip(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HealthTipId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("ManageHealthTips");
                }


                string imageQuery = @"

SELECT TipImage

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId";

                SqlCommand imageCmd = new SqlCommand(imageQuery, con);

                imageCmd.Parameters.AddWithValue("@HealthTipId", id);

                object imageObj = imageCmd.ExecuteScalar();

                string imageName = "";

                if (imageObj != null && imageObj != DBNull.Value)
                {
                    imageName = imageObj.ToString();
                }


                string deleteQuery = @"

UPDATE tbl_HealthTip

SET

IsDeleted=@IsDeleted,
UpdatedDate=@UpdatedDate

WHERE

HealthTipId=@HealthTipId";

                SqlCommand cmd = new SqlCommand(deleteQuery, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                cmd.Parameters.AddWithValue("@IsDeleted", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        string imagePath = Path.Combine(env.WebRootPath, "HealthTips", imageName);

                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    TempData["Success"] = "Health Tip deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete Health Tip.";
                }
            }

            return RedirectToAction("ManageHealthTips");
        }
      
        [HttpGet]
        public IActionResult ManageDoctorLeave(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DoctorLeaveModel> list = new List<DoctorLeaveModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

DL.LeaveId,
DL.DoctorId,
DL.LeaveFromDate,
DL.LeaveToDate,
DL.LeaveReason,
DL.IsActive,
DL.IsDeleted,
DL.CreatedDate,
DL.UpdatedDate,

D.DoctorName,

DP.DepartmentName

FROM tbl_DoctorLeave DL

INNER JOIN tbl_Doctor D
ON DL.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

DL.IsDeleted = 0

AND
(
    @Search = ''

    OR D.DoctorName LIKE @SearchText

    OR DP.DepartmentName LIKE @SearchText

    OR DL.LeaveReason LIKE @SearchText
)

ORDER BY

DL.LeaveId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    DoctorLeaveModel model = new DoctorLeaveModel();

                    model.LeaveId = Convert.ToInt64(dr["LeaveId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                                            ? ""
                                            : dr["DepartmentName"].ToString();

                    model.LeaveFromDate = Convert.ToDateTime(dr["LeaveFromDate"]);

                    model.LeaveToDate = Convert.ToDateTime(dr["LeaveToDate"]);

                    model.LeaveReason = dr["LeaveReason"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
       
        [HttpGet]
        public IActionResult AddDoctorLeave()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorLeaveModel model = new DoctorLeaveModel();

            model.DoctorList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string query = @"

SELECT

DoctorId,
DoctorName

FROM tbl_Doctor

WHERE

IsActive = 1
AND IsDeleted = 0

ORDER BY

DoctorName ASC";

                SqlCommand cmd = new SqlCommand(query, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = dr["DoctorId"].ToString(),
                        Text = dr["DoctorName"].ToString()
                    });
                }

                dr.Close();
            }


            model.LeaveFromDate = DateTime.Today;

            model.LeaveToDate = DateTime.Today;

            model.IsActive = true;

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDoctorLeave(DoctorLeaveModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            model.DoctorList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string doctorQuery = @"

SELECT
DoctorId,
DoctorName

FROM tbl_Doctor

WHERE
IsActive=1
AND IsDeleted=0

ORDER BY DoctorName";

                SqlCommand doctorCmd = new SqlCommand(doctorQuery, con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();


                if (model.LeaveToDate < model.LeaveFromDate)
                {
                    ModelState.AddModelError("", "Leave To Date must be greater than or equal to Leave From Date.");

                    return View(model);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_DoctorLeave

WHERE

DoctorId=@DoctorId
AND IsDeleted=0
AND IsActive=1

AND

(
    @LeaveFromDate BETWEEN LeaveFromDate AND LeaveToDate

    OR

    @LeaveToDate BETWEEN LeaveFromDate AND LeaveToDate

    OR

    LeaveFromDate BETWEEN @LeaveFromDate AND @LeaveToDate
)";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                checkCmd.Parameters.AddWithValue("@LeaveFromDate", model.LeaveFromDate);

                checkCmd.Parameters.AddWithValue("@LeaveToDate", model.LeaveToDate);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Doctor already has a leave record for the selected dates.";

                    return View(model);
                }


                string insertQuery = @"

INSERT INTO tbl_DoctorLeave
(
DoctorId,
LeaveFromDate,
LeaveToDate,
LeaveReason,
IsActive,
IsDeleted,
CreatedDate
)

VALUES
(
@DoctorId,
@LeaveFromDate,
@LeaveToDate,
@LeaveReason,
@IsActive,
@IsDeleted,
@CreatedDate
)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);

                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                cmd.Parameters.AddWithValue("@LeaveFromDate", model.LeaveFromDate);

                cmd.Parameters.AddWithValue("@LeaveToDate", model.LeaveToDate);

                cmd.Parameters.AddWithValue("@LeaveReason", model.LeaveReason);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Doctor leave added successfully.";

                    return RedirectToAction("ManageDoctorLeave");
                }

                TempData["Error"] = "Unable to add doctor leave.";

                return View(model);
            }
        }
      
        [HttpGet]
        public IActionResult EditDoctorLeave(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorLeaveModel model = new DoctorLeaveModel();

            model.DoctorList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string doctorQuery = @"

SELECT
DoctorId,
DoctorName

FROM tbl_Doctor

WHERE
IsActive=1
AND IsDeleted=0

ORDER BY DoctorName";

                SqlCommand doctorCmd = new SqlCommand(doctorQuery, con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();


                string query = @"

SELECT

LeaveId,
DoctorId,
LeaveFromDate,
LeaveToDate,
LeaveReason,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_DoctorLeave

WHERE

LeaveId=@LeaveId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@LeaveId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.LeaveId = Convert.ToInt64(dr["LeaveId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.LeaveFromDate = Convert.ToDateTime(dr["LeaveFromDate"]);

                    model.LeaveToDate = Convert.ToDateTime(dr["LeaveToDate"]);

                    model.LeaveReason = dr["LeaveReason"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Doctor leave record not found.";

                    return RedirectToAction("ManageDoctorLeave");
                }

                dr.Close();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoctorLeave(DoctorLeaveModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            model.DoctorList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string doctorQuery = @"

SELECT
DoctorId,
DoctorName

FROM tbl_Doctor

WHERE
IsActive=1
AND IsDeleted=0

ORDER BY DoctorName";

                SqlCommand doctorCmd = new SqlCommand(doctorQuery, con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();


                if (model.LeaveToDate < model.LeaveFromDate)
                {
                    ModelState.AddModelError("", "Leave To Date must be greater than or equal to Leave From Date.");

                    return View(model);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_DoctorLeave

WHERE

DoctorId=@DoctorId
AND LeaveId<>@LeaveId
AND IsDeleted=0
AND IsActive=1

AND
(
    @LeaveFromDate BETWEEN LeaveFromDate AND LeaveToDate

    OR

    @LeaveToDate BETWEEN LeaveFromDate AND LeaveToDate

    OR

    LeaveFromDate BETWEEN @LeaveFromDate AND @LeaveToDate

    OR

    LeaveToDate BETWEEN @LeaveFromDate AND @LeaveToDate
)";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@LeaveId", model.LeaveId);

                checkCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                checkCmd.Parameters.AddWithValue("@LeaveFromDate", model.LeaveFromDate);

                checkCmd.Parameters.AddWithValue("@LeaveToDate", model.LeaveToDate);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Doctor already has another leave during the selected dates.";

                    return View(model);
                }


                string updateQuery = @"

UPDATE tbl_DoctorLeave

SET

DoctorId=@DoctorId,
LeaveFromDate=@LeaveFromDate,
LeaveToDate=@LeaveToDate,
LeaveReason=@LeaveReason,
IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

LeaveId=@LeaveId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@LeaveId", model.LeaveId);

                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                cmd.Parameters.AddWithValue("@LeaveFromDate", model.LeaveFromDate);

                cmd.Parameters.AddWithValue("@LeaveToDate", model.LeaveToDate);

                cmd.Parameters.AddWithValue("@LeaveReason", model.LeaveReason);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Doctor leave updated successfully.";

                    return RedirectToAction("ManageDoctorLeave");
                }

                TempData["Error"] = "Unable to update doctor leave.";

                return View(model);
            }
        }
       
        [HttpGet]
        public IActionResult ViewDoctorLeave(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorLeaveModel model = new DoctorLeaveModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

DL.LeaveId,
DL.DoctorId,
DL.LeaveFromDate,
DL.LeaveToDate,
DL.LeaveReason,
DL.IsActive,
DL.IsDeleted,
DL.CreatedDate,
DL.UpdatedDate,

D.DoctorName,

DP.DepartmentName

FROM tbl_DoctorLeave DL

INNER JOIN tbl_Doctor D
ON DL.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

DL.LeaveId=@LeaveId
AND DL.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@LeaveId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.LeaveId = Convert.ToInt64(dr["LeaveId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.LeaveFromDate = Convert.ToDateTime(dr["LeaveFromDate"]);

                    model.LeaveToDate = Convert.ToDateTime(dr["LeaveToDate"]);

                    model.LeaveReason = dr["LeaveReason"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Doctor leave record not found.";

                    return RedirectToAction("ManageDoctorLeave");
                }

                dr.Close();
            }

            return View(model);
        }
     
        [HttpGet]
        public IActionResult ChangeLeaveStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsActive

FROM tbl_DoctorLeave

WHERE

LeaveId=@LeaveId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@LeaveId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Doctor leave record not found.";

                    return RedirectToAction("ManageDoctorLeave");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;

                string updateQuery = @"

UPDATE tbl_DoctorLeave

SET

IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

LeaveId=@LeaveId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@LeaveId", id);

                cmd.Parameters.AddWithValue("@IsActive", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Doctor leave activated successfully."
                        : "Doctor leave deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update doctor leave status.";
                }
            }

            return RedirectToAction("ManageDoctorLeave");
        }
     
        [HttpGet]
        public IActionResult DeleteDoctorLeave(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_DoctorLeave

WHERE

LeaveId=@LeaveId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@LeaveId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Doctor leave record not found.";

                    return RedirectToAction("ManageDoctorLeave");
                }


                string deleteQuery = @"

UPDATE tbl_DoctorLeave

SET

IsDeleted=@IsDeleted,
UpdatedDate=@UpdatedDate

WHERE

LeaveId=@LeaveId";

                SqlCommand cmd = new SqlCommand(deleteQuery, con);

                cmd.Parameters.AddWithValue("@LeaveId", id);

                cmd.Parameters.AddWithValue("@IsDeleted", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Doctor leave deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete doctor leave.";
                }
            }

            return RedirectToAction("ManageDoctorLeave");
        }
       
        [HttpGet]
        public IActionResult ManageHospitalHoliday(string search = "", string holidayDate = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<HospitalHolidayModel> list = new List<HospitalHolidayModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HolidayId,
HolidayTitle,
HolidayDate,
HolidayDescription,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HospitalHoliday

WHERE

IsDeleted = 0
";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += " AND HolidayTitle LIKE @Search";
                }

                if (!string.IsNullOrWhiteSpace(holidayDate))
                {
                    query += " AND CONVERT(date, HolidayDate)=@HolidayDate";
                }

                query += " ORDER BY HolidayDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                }

                if (!string.IsNullOrWhiteSpace(holidayDate))
                {
                    cmd.Parameters.AddWithValue("@HolidayDate", Convert.ToDateTime(holidayDate));
                }

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    HospitalHolidayModel model = new HospitalHolidayModel();

                    model.HolidayId = Convert.ToInt64(dr["HolidayId"]);

                    model.HolidayTitle = dr["HolidayTitle"].ToString();

                    model.HolidayDate = Convert.ToDateTime(dr["HolidayDate"]);

                    model.HolidayDescription = dr["HolidayDescription"] == DBNull.Value
                        ? ""
                        : dr["HolidayDescription"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            ViewBag.HolidayDate = holidayDate;

            return View(list);
        }
       
        [HttpGet]
        public IActionResult AddHospitalHoliday()
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HospitalHolidayModel model = new HospitalHolidayModel();


            model.HolidayDate = DateTime.Today;

            model.IsActive = true;

            return View(model);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddHospitalHoliday(HospitalHolidayModel model)
        {
           

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_HospitalHoliday

WHERE

HolidayDate=@HolidayDate
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HolidayDate", model.HolidayDate.Date);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "A holiday already exists for the selected date.";

                    return View(model);
                }

                string insertQuery = @"

INSERT INTO tbl_HospitalHoliday
(
HolidayTitle,
HolidayDate,
HolidayDescription,
IsActive,
IsDeleted,
CreatedDate
)

VALUES
(
@HolidayTitle,
@HolidayDate,
@HolidayDescription,
@IsActive,
@IsDeleted,
@CreatedDate
)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);

                cmd.Parameters.AddWithValue("@HolidayTitle", model.HolidayTitle);

                cmd.Parameters.AddWithValue("@HolidayDate", model.HolidayDate.Date);

                cmd.Parameters.AddWithValue("@HolidayDescription",
                    string.IsNullOrWhiteSpace(model.HolidayDescription)
                    ? DBNull.Value
                    : (object)model.HolidayDescription);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Hospital holiday added successfully.";

                    return RedirectToAction("ManageHospitalHoliday");
                }
                else
                {
                    TempData["Error"] = "Unable to add hospital holiday.";

                    return View(model);
                }
            }
        }
     
        [HttpGet]
        public IActionResult EditHospitalHoliday(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HospitalHolidayModel model = new HospitalHolidayModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HolidayId,
HolidayTitle,
HolidayDate,
HolidayDescription,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HospitalHoliday

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@HolidayId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.HolidayId = Convert.ToInt64(dr["HolidayId"]);

                    model.HolidayTitle = dr["HolidayTitle"].ToString();

                    model.HolidayDate = Convert.ToDateTime(dr["HolidayDate"]);

                    model.HolidayDescription = dr["HolidayDescription"] == DBNull.Value
                        ? ""
                        : dr["HolidayDescription"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Hospital holiday record not found.";

                    return RedirectToAction("ManageHospitalHoliday");
                }

                dr.Close();
            }

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditHospitalHoliday(HospitalHolidayModel model)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_HospitalHoliday

WHERE

HolidayDate=@HolidayDate
AND HolidayId<>@HolidayId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HolidayDate", model.HolidayDate.Date);

                checkCmd.Parameters.AddWithValue("@HolidayId", model.HolidayId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Another holiday already exists for the selected date.";

                    return View(model);
                }

                string updateQuery = @"

UPDATE tbl_HospitalHoliday

SET

HolidayTitle=@HolidayTitle,
HolidayDate=@HolidayDate,
HolidayDescription=@HolidayDescription,
IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@HolidayId", model.HolidayId);

                cmd.Parameters.AddWithValue("@HolidayTitle", model.HolidayTitle);

                cmd.Parameters.AddWithValue("@HolidayDate", model.HolidayDate.Date);

                cmd.Parameters.AddWithValue("@HolidayDescription",
                    string.IsNullOrWhiteSpace(model.HolidayDescription)
                    ? DBNull.Value
                    : (object)model.HolidayDescription);

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Hospital holiday updated successfully.";

                    return RedirectToAction("ManageHospitalHoliday");
                }
                else
                {
                    TempData["Error"] = "Unable to update hospital holiday.";

                    return View(model);
                }
            }
        }
      
        [HttpGet]
        public IActionResult ViewHospitalHoliday(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            HospitalHolidayModel model = new HospitalHolidayModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HolidayId,
HolidayTitle,
HolidayDate,
HolidayDescription,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_HospitalHoliday

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@HolidayId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.HolidayId = Convert.ToInt64(dr["HolidayId"]);

                    model.HolidayTitle = dr["HolidayTitle"].ToString();

                    model.HolidayDate = Convert.ToDateTime(dr["HolidayDate"]);

                    model.HolidayDescription = dr["HolidayDescription"] == DBNull.Value
                        ? ""
                        : dr["HolidayDescription"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Hospital holiday record not found.";

                    return RedirectToAction("ManageHospitalHoliday");
                }

                dr.Close();
            }

            return View(model);
        }
  
        [HttpGet]
        public IActionResult ChangeHolidayStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT IsActive

FROM tbl_HospitalHoliday

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HolidayId", id);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Hospital holiday record not found.";

                    return RedirectToAction("ManageHospitalHoliday");
                }

                bool currentStatus = Convert.ToBoolean(result);

                bool newStatus = !currentStatus;


                string updateQuery = @"

UPDATE tbl_HospitalHoliday

SET

IsActive=@IsActive,
UpdatedDate=@UpdatedDate

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@HolidayId", id);

                cmd.Parameters.AddWithValue("@IsActive", newStatus);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int update = cmd.ExecuteNonQuery();

                if (update > 0)
                {
                    TempData["Success"] = newStatus
                        ? "Hospital holiday activated successfully."
                        : "Hospital holiday deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update hospital holiday status.";
                }
            }

            return RedirectToAction("ManageHospitalHoliday");
        }
    
        [HttpGet]
        public IActionResult DeleteHospitalHoliday(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_HospitalHoliday

WHERE

HolidayId=@HolidayId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@HolidayId", id);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                {
                    TempData["Error"] = "Hospital holiday record not found.";

                    return RedirectToAction("ManageHospitalHoliday");
                }

                string deleteQuery = @"

UPDATE tbl_HospitalHoliday

SET

IsDeleted=@IsDeleted,
UpdatedDate=@UpdatedDate

WHERE

HolidayId=@HolidayId";

                SqlCommand cmd = new SqlCommand(deleteQuery, con);

                cmd.Parameters.AddWithValue("@HolidayId", id);

                cmd.Parameters.AddWithValue("@IsDeleted", true);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Hospital holiday deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete hospital holiday.";
                }
            }

            return RedirectToAction("ManageHospitalHoliday");
        }
        //======================================================
        // Clinic Information - GET
        //======================================================
        [HttpGet]
        public IActionResult ClinicInformation()
        {
            //=========================================
            // Admin Login Check
            //=========================================

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ClinicInformationModel model = new ClinicInformationModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT TOP 1

ClinicId,
ClinicName,
AboutClinic,
Mission,
Vision,
WhyChooseUs,
Address,
MobileNo,
AlternateMobileNo,
Email,
Website,
WorkingHours,
EmergencyContact,
GoogleMapLink,
FacebookLink,
InstagramLink,
TwitterLink,
WhatsAppNo,
ClinicLogo,
BannerImage,
IsActive,
CreatedDate,
UpdatedDate

FROM tbl_ClinicInformation

ORDER BY ClinicId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.ClinicId = Convert.ToInt64(dr["ClinicId"]);

                    model.ClinicName = dr["ClinicName"].ToString();

                    model.AboutClinic = dr["AboutClinic"].ToString();

                    model.Mission = dr["Mission"] == DBNull.Value ? "" : dr["Mission"].ToString();

                    model.Vision = dr["Vision"] == DBNull.Value ? "" : dr["Vision"].ToString();

                    model.WhyChooseUs = dr["WhyChooseUs"] == DBNull.Value ? "" : dr["WhyChooseUs"].ToString();

                    model.Address = dr["Address"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.AlternateMobileNo = dr["AlternateMobileNo"] == DBNull.Value
                        ? ""
                        : dr["AlternateMobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.Website = dr["Website"] == DBNull.Value
                        ? ""
                        : dr["Website"].ToString();

                    model.WorkingHours = dr["WorkingHours"].ToString();

                    model.EmergencyContact = dr["EmergencyContact"] == DBNull.Value
                        ? ""
                        : dr["EmergencyContact"].ToString();

                    model.GoogleMapLink = dr["GoogleMapLink"] == DBNull.Value
                        ? ""
                        : dr["GoogleMapLink"].ToString();

                    model.FacebookLink = dr["FacebookLink"] == DBNull.Value
                        ? ""
                        : dr["FacebookLink"].ToString();

                    model.InstagramLink = dr["InstagramLink"] == DBNull.Value
                        ? ""
                        : dr["InstagramLink"].ToString();

                    model.TwitterLink = dr["TwitterLink"] == DBNull.Value
                        ? ""
                        : dr["TwitterLink"].ToString();

                    model.WhatsAppNo = dr["WhatsAppNo"] == DBNull.Value
                        ? ""
                        : dr["WhatsAppNo"].ToString();

                    model.ClinicLogo = dr["ClinicLogo"] == DBNull.Value
                        ? ""
                        : dr["ClinicLogo"].ToString();

                    model.BannerImage = dr["BannerImage"] == DBNull.Value
                        ? ""
                        : dr["BannerImage"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }

                dr.Close();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClinicInformation(ClinicInformationModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    if (model.ClinicLogoFile != null &&
                        model.ClinicLogoFile.Length > 0)
                    {
                        string folderPath = Path.Combine(env.WebRootPath, "Clinic");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string logoFileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.ClinicLogoFile.FileName);

                        string logoPath =
                            Path.Combine(folderPath, logoFileName);

                        using (FileStream fs = new FileStream(logoPath, FileMode.Create))
                        {
                            model.ClinicLogoFile.CopyTo(fs);
                        }

                        model.ClinicLogo = logoFileName;
                    }


                    if (model.BannerImageFile != null &&
                        model.BannerImageFile.Length > 0)
                    {
                        string folderPath = Path.Combine(env.WebRootPath, "Clinic");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string bannerFileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.BannerImageFile.FileName);

                        string bannerPath =
                            Path.Combine(folderPath, bannerFileName);

                        using (FileStream fs = new FileStream(bannerPath, FileMode.Create))
                        {
                            model.BannerImageFile.CopyTo(fs);
                        }

                        model.BannerImage = bannerFileName;
                    }


                    string checkQuery = @"

SELECT COUNT(*)

FROM tbl_ClinicInformation";

                    SqlCommand checkCmd =
                        new SqlCommand(checkQuery, con);

                    int count =
                        Convert.ToInt32(checkCmd.ExecuteScalar());
                

                    if (count == 0)
                    {
                        string insertQuery = @"

INSERT INTO tbl_ClinicInformation
(
ClinicName,
AboutClinic,
Mission,
Vision,
WhyChooseUs,
Address,
MobileNo,
AlternateMobileNo,
Email,
Website,
WorkingHours,
EmergencyContact,
GoogleMapLink,
FacebookLink,
InstagramLink,
TwitterLink,
WhatsAppNo,
ClinicLogo,
BannerImage,
IsActive,
CreatedDate
)

VALUES
(
@ClinicName,
@AboutClinic,
@Mission,
@Vision,
@WhyChooseUs,
@Address,
@MobileNo,
@AlternateMobileNo,
@Email,
@Website,
@WorkingHours,
@EmergencyContact,
@GoogleMapLink,
@FacebookLink,
@InstagramLink,
@TwitterLink,
@WhatsAppNo,
@ClinicLogo,
@BannerImage,
@IsActive,
@CreatedDate
)";

                        SqlCommand cmd = new SqlCommand(insertQuery, con);

                        cmd.Parameters.AddWithValue("@ClinicName", model.ClinicName);

                        cmd.Parameters.AddWithValue("@AboutClinic", model.AboutClinic);

                        cmd.Parameters.AddWithValue("@Mission",
                            string.IsNullOrWhiteSpace(model.Mission)
                            ? DBNull.Value
                            : (object)model.Mission);

                        cmd.Parameters.AddWithValue("@Vision",
                            string.IsNullOrWhiteSpace(model.Vision)
                            ? DBNull.Value
                            : (object)model.Vision);

                        cmd.Parameters.AddWithValue("@WhyChooseUs",
                            string.IsNullOrWhiteSpace(model.WhyChooseUs)
                            ? DBNull.Value
                            : (object)model.WhyChooseUs);

                        cmd.Parameters.AddWithValue("@Address", model.Address);

                        cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);

                        cmd.Parameters.AddWithValue("@AlternateMobileNo",
                            string.IsNullOrWhiteSpace(model.AlternateMobileNo)
                            ? DBNull.Value
                            : (object)model.AlternateMobileNo);

                        cmd.Parameters.AddWithValue("@Email", model.Email);

                        cmd.Parameters.AddWithValue("@Website",
                            string.IsNullOrWhiteSpace(model.Website)
                            ? DBNull.Value
                            : (object)model.Website);

                        cmd.Parameters.AddWithValue("@WorkingHours", model.WorkingHours);

                        cmd.Parameters.AddWithValue("@EmergencyContact",
                            string.IsNullOrWhiteSpace(model.EmergencyContact)
                            ? DBNull.Value
                            : (object)model.EmergencyContact);

                        cmd.Parameters.AddWithValue("@GoogleMapLink",
                            string.IsNullOrWhiteSpace(model.GoogleMapLink)
                            ? DBNull.Value
                            : (object)model.GoogleMapLink);

                        cmd.Parameters.AddWithValue("@FacebookLink",
                            string.IsNullOrWhiteSpace(model.FacebookLink)
                            ? DBNull.Value
                            : (object)model.FacebookLink);

                        cmd.Parameters.AddWithValue("@InstagramLink",
                            string.IsNullOrWhiteSpace(model.InstagramLink)
                            ? DBNull.Value
                            : (object)model.InstagramLink);

                        cmd.Parameters.AddWithValue("@TwitterLink",
                            string.IsNullOrWhiteSpace(model.TwitterLink)
                            ? DBNull.Value
                            : (object)model.TwitterLink);

                        cmd.Parameters.AddWithValue("@WhatsAppNo",
                            string.IsNullOrWhiteSpace(model.WhatsAppNo)
                            ? DBNull.Value
                            : (object)model.WhatsAppNo);

                        cmd.Parameters.AddWithValue("@ClinicLogo",
                            string.IsNullOrWhiteSpace(model.ClinicLogo)
                            ? DBNull.Value
                            : (object)model.ClinicLogo);

                        cmd.Parameters.AddWithValue("@BannerImage",
                            string.IsNullOrWhiteSpace(model.BannerImage)
                            ? DBNull.Value
                            : (object)model.BannerImage);

                        cmd.Parameters.AddWithValue("@IsActive", true);

                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            TempData["Success"] = "Clinic information saved successfully.";

                            return RedirectToAction("ClinicInformation");
                        }
                        else
                        {
                            TempData["Error"] = "Unable to save clinic information.";

                            return View(model);
                        }
                    }
                  

                    else
                    {

                        string oldLogo = "";
                        string oldBanner = "";

                        SqlCommand oldCmd = new SqlCommand(@"

SELECT TOP 1

ClinicLogo,
BannerImage

FROM tbl_ClinicInformation", con);

                        SqlDataReader oldDr = oldCmd.ExecuteReader();

                        if (oldDr.Read())
                        {
                            oldLogo = oldDr["ClinicLogo"] == DBNull.Value
                                ? ""
                                : oldDr["ClinicLogo"].ToString();

                            oldBanner = oldDr["BannerImage"] == DBNull.Value
                                ? ""
                                : oldDr["BannerImage"].ToString();
                        }

                        oldDr.Close();


                        if (string.IsNullOrWhiteSpace(model.ClinicLogo))
                        {
                            model.ClinicLogo = oldLogo;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(oldLogo))
                            {
                                string oldLogoPath = Path.Combine(env.WebRootPath,
                                    "Clinic",
                                    oldLogo);

                                if (System.IO.File.Exists(oldLogoPath))
                                {
                                    System.IO.File.Delete(oldLogoPath);
                                }
                            }
                        }


                        if (string.IsNullOrWhiteSpace(model.BannerImage))
                        {
                            model.BannerImage = oldBanner;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(oldBanner))
                            {
                                string oldBannerPath = Path.Combine(env.WebRootPath,
                                    "Clinic",
                                    oldBanner);

                                if (System.IO.File.Exists(oldBannerPath))
                                {
                                    System.IO.File.Delete(oldBannerPath);
                                }
                            }
                        }


                        string updateQuery = @"

UPDATE tbl_ClinicInformation

SET

ClinicName=@ClinicName,
AboutClinic=@AboutClinic,
Mission=@Mission,
Vision=@Vision,
WhyChooseUs=@WhyChooseUs,
Address=@Address,
MobileNo=@MobileNo,
AlternateMobileNo=@AlternateMobileNo,
Email=@Email,
Website=@Website,
WorkingHours=@WorkingHours,
EmergencyContact=@EmergencyContact,
GoogleMapLink=@GoogleMapLink,
FacebookLink=@FacebookLink,
InstagramLink=@InstagramLink,
TwitterLink=@TwitterLink,
WhatsAppNo=@WhatsAppNo,
ClinicLogo=@ClinicLogo,
BannerImage=@BannerImage,
UpdatedDate=@UpdatedDate";

                        SqlCommand cmd = new SqlCommand(updateQuery, con);

                        cmd.Parameters.AddWithValue("@ClinicName", model.ClinicName);
                        cmd.Parameters.AddWithValue("@AboutClinic", model.AboutClinic);
                        cmd.Parameters.AddWithValue("@Mission", (object?)model.Mission ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Vision", (object?)model.Vision ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WhyChooseUs", (object?)model.WhyChooseUs ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", model.Address);
                        cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                        cmd.Parameters.AddWithValue("@AlternateMobileNo", (object?)model.AlternateMobileNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@Website", (object?)model.Website ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WorkingHours", model.WorkingHours);
                        cmd.Parameters.AddWithValue("@EmergencyContact", (object?)model.EmergencyContact ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GoogleMapLink", (object?)model.GoogleMapLink ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FacebookLink", (object?)model.FacebookLink ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@InstagramLink", (object?)model.InstagramLink ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TwitterLink", (object?)model.TwitterLink ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WhatsAppNo", (object?)model.WhatsAppNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ClinicLogo", (object?)model.ClinicLogo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BannerImage", (object?)model.BannerImage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            TempData["Success"] = "Clinic information updated successfully.";

                            return RedirectToAction("ClinicInformation");
                        }
                        else
                        {
                            TempData["Error"] = "Unable to update clinic information.";

                            return View(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return View(model);
            }
        }
      
        [HttpGet]
        public IActionResult ManageMedicalCamp(string search = "")
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<MedicalCampModel> list = new List<MedicalCampModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

mc.CampId,
mc.CampTitle,
mc.CampImage,
mc.CampDate,
mc.StartTime,
mc.EndTime,
mc.Venue,
mc.RegistrationFee,
mc.MaxParticipants,
mc.AvailableSeats,
mc.IsFeatured,
mc.IsActive,
mc.CreatedDate,

d.DoctorName,

dep.DepartmentName

FROM tbl_MedicalCamp mc

INNER JOIN tbl_Doctor d
ON mc.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON mc.DepartmentId = dep.DepartmentId

WHERE

mc.IsDeleted = 0

";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += @"

AND
(
    mc.CampTitle LIKE @Search
    OR d.DoctorName LIKE @Search
    OR dep.DepartmentName LIKE @Search
)

";
                }

                query += @"

ORDER BY
mc.CampDate DESC,
mc.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                }

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    MedicalCampModel model = new MedicalCampModel();

                    model.CampId = Convert.ToInt64(dr["CampId"]);

                    model.CampTitle = dr["CampTitle"].ToString();

                    model.CampImage = dr["CampImage"] == DBNull.Value
                        ? ""
                        : dr["CampImage"].ToString();

                    model.CampDate = Convert.ToDateTime(dr["CampDate"]);

                    model.StartTime = (TimeSpan)dr["StartTime"];

                    model.EndTime = (TimeSpan)dr["EndTime"];

                    model.Venue = dr["Venue"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.RegistrationFee = Convert.ToDecimal(dr["RegistrationFee"]);

                    model.MaxParticipants = Convert.ToInt32(dr["MaxParticipants"]);

                    model.AvailableSeats = Convert.ToInt32(dr["AvailableSeats"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
      
        [HttpGet]
        public IActionResult AddMedicalCamp()
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            MedicalCampModel model = new MedicalCampModel();

            model.DoctorList = new List<SelectListItem>();
            model.DepartmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                SqlCommand doctorCmd = new SqlCommand(@"

SELECT

DoctorId,
DoctorName

FROM tbl_Doctor

WHERE

IsActive=1
AND IsDeleted=0

ORDER BY DoctorName", con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();


                SqlCommand deptCmd = new SqlCommand(@"

SELECT

DepartmentId,
DepartmentName

FROM tbl_Department

WHERE

IsActive=1
AND IsDeleted=0

ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                while (deptDr.Read())
                {
                    model.DepartmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }


            model.CampDate = DateTime.Today;

            model.RegistrationFee = 0;

            model.IsActive = true;

            model.IsFeatured = false;

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMedicalCamp(MedicalCampModel model)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            model.DoctorList = new List<SelectListItem>();
            model.DepartmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand doctorCmd = new SqlCommand(@"
SELECT DoctorId, DoctorName
FROM tbl_Doctor
WHERE IsActive=1
AND IsDeleted=0
ORDER BY DoctorName", con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();

                SqlCommand deptCmd = new SqlCommand(@"
SELECT DepartmentId, DepartmentName
FROM tbl_Department
WHERE IsActive=1
AND IsDeleted=0
ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                while (deptDr.Read())
                {
                    model.DepartmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }


            ModelState.Remove(nameof(MedicalCampModel.DoctorName));
            ModelState.Remove(nameof(MedicalCampModel.DepartmentName));
            ModelState.Remove(nameof(MedicalCampModel.CampImage));
            ModelState.Remove(nameof(MedicalCampModel.CampBanner));
            ModelState.Remove(nameof(MedicalCampModel.AvailableSeats));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    if (model.CampImageFile != null &&
                        model.CampImageFile.Length > 0)
                    {
                        string folderPath = Path.Combine(env.WebRootPath, "MedicalCamp");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.CampImageFile.FileName);

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            model.CampImageFile.CopyTo(fs);
                        }

                        model.CampImage = fileName;
                    }


                    if (model.CampBannerFile != null &&
                        model.CampBannerFile.Length > 0)
                    {
                        string folderPath = Path.Combine(env.WebRootPath, "MedicalCamp");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.CampBannerFile.FileName);

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            model.CampBannerFile.CopyTo(fs);
                        }

                        model.CampBanner = fileName;
                    }
                 

                    SqlCommand checkCmd = new SqlCommand(@"

SELECT COUNT(*)

FROM tbl_MedicalCamp

WHERE

CampTitle=@CampTitle
AND CampDate=@CampDate
AND IsDeleted=0", con);

                    checkCmd.Parameters.AddWithValue("@CampTitle", model.CampTitle);

                    checkCmd.Parameters.AddWithValue("@CampDate", model.CampDate);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        TempData["Error"] = "Medical Camp already exists for this date.";

                        return View(model);
                    }


                    model.AvailableSeats = model.MaxParticipants;


                    string query = @"

INSERT INTO tbl_MedicalCamp
(
CampTitle,
CampImage,
CampBanner,
CampDescription,
CampDate,
StartTime,
EndTime,
Venue,
Organizer,
DoctorId,
DepartmentId,
RegistrationFee,
MaxParticipants,
AvailableSeats,
ContactNumber,
Email,
Benefits,
Instructions,
IsFeatured,
IsActive,
IsDeleted,
CreatedDate
)

VALUES
(
@CampTitle,
@CampImage,
@CampBanner,
@CampDescription,
@CampDate,
@StartTime,
@EndTime,
@Venue,
@Organizer,
@DoctorId,
@DepartmentId,
@RegistrationFee,
@MaxParticipants,
@AvailableSeats,
@ContactNumber,
@Email,
@Benefits,
@Instructions,
@IsFeatured,
@IsActive,
@IsDeleted,
@CreatedDate
)";

                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CampTitle", model.CampTitle);

                    cmd.Parameters.AddWithValue("@CampImage",
                        string.IsNullOrWhiteSpace(model.CampImage)
                        ? DBNull.Value
                        : (object)model.CampImage);

                    cmd.Parameters.AddWithValue("@CampBanner",
                        string.IsNullOrWhiteSpace(model.CampBanner)
                        ? DBNull.Value
                        : (object)model.CampBanner);

                    cmd.Parameters.AddWithValue("@CampDescription", model.CampDescription);

                    cmd.Parameters.AddWithValue("@CampDate", model.CampDate);

                    cmd.Parameters.AddWithValue("@StartTime", model.StartTime);

                    cmd.Parameters.AddWithValue("@EndTime", model.EndTime);

                    cmd.Parameters.AddWithValue("@Venue", model.Venue);

                    cmd.Parameters.AddWithValue("@Organizer",
                        string.IsNullOrWhiteSpace(model.Organizer)
                        ? DBNull.Value
                        : (object)model.Organizer);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    cmd.Parameters.AddWithValue("@RegistrationFee", model.RegistrationFee);

                    cmd.Parameters.AddWithValue("@MaxParticipants", model.MaxParticipants);

                    cmd.Parameters.AddWithValue("@AvailableSeats", model.AvailableSeats);

                    cmd.Parameters.AddWithValue("@ContactNumber", model.ContactNumber);

                    cmd.Parameters.AddWithValue("@Email",
                        string.IsNullOrWhiteSpace(model.Email)
                        ? DBNull.Value
                        : (object)model.Email);

                    cmd.Parameters.AddWithValue("@Benefits",
                        string.IsNullOrWhiteSpace(model.Benefits)
                        ? DBNull.Value
                        : (object)model.Benefits);

                    cmd.Parameters.AddWithValue("@Instructions",
                        string.IsNullOrWhiteSpace(model.Instructions)
                        ? DBNull.Value
                        : (object)model.Instructions);

                    cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);

                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                    cmd.Parameters.AddWithValue("@IsDeleted", false);

                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                  

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Medical Camp added successfully.";

                        return RedirectToAction("ManageMedicalCamp");
                    }
                    else
                    {
                        TempData["Error"] = "Unable to add medical camp.";

                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;


                model.DoctorList = new List<SelectListItem>();
                model.DepartmentList = new List<SelectListItem>();

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                   
                    SqlCommand doctorCmd = new SqlCommand(@"
SELECT DoctorId, DoctorName
FROM tbl_Doctor
WHERE IsActive = 1
AND IsDeleted = 0
ORDER BY DoctorName", con);

                    SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                    while (doctorDr.Read())
                    {
                        model.DoctorList.Add(new SelectListItem
                        {
                            Value = doctorDr["DoctorId"].ToString(),
                            Text = doctorDr["DoctorName"].ToString()
                        });
                    }

                    doctorDr.Close();

                    
                    SqlCommand deptCmd = new SqlCommand(@"
SELECT DepartmentId, DepartmentName
FROM tbl_Department
WHERE IsActive = 1
AND IsDeleted = 0
ORDER BY DepartmentName", con);

                    SqlDataReader deptDr = deptCmd.ExecuteReader();

                    while (deptDr.Read())
                    {
                        model.DepartmentList.Add(new SelectListItem
                        {
                            Value = deptDr["DepartmentId"].ToString(),
                            Text = deptDr["DepartmentName"].ToString()
                        });
                    }

                    deptDr.Close();
                }

                return View(model);
            }
        }
     
        [HttpGet]
        public IActionResult EditMedicalCamp(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            MedicalCampModel model = new MedicalCampModel();

            model.DoctorList = new List<SelectListItem>();
            model.DepartmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                SqlCommand doctorCmd = new SqlCommand(@"

SELECT
DoctorId,
DoctorName

FROM tbl_Doctor

WHERE
IsActive=1
AND IsDeleted=0

ORDER BY DoctorName", con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();


                SqlCommand deptCmd = new SqlCommand(@"

SELECT
DepartmentId,
DepartmentName

FROM tbl_Department

WHERE
IsActive=1
AND IsDeleted=0

ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                while (deptDr.Read())
                {
                    model.DepartmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();


                SqlCommand cmd = new SqlCommand(@"

SELECT *

FROM tbl_MedicalCamp

WHERE
CampId=@CampId
AND IsDeleted=0", con);

                cmd.Parameters.AddWithValue("@CampId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.CampId = Convert.ToInt64(dr["CampId"]);

                    model.CampTitle = dr["CampTitle"].ToString();

                    model.CampDescription = dr["CampDescription"].ToString();

                    model.CampDate = Convert.ToDateTime(dr["CampDate"]);

                    model.StartTime = (TimeSpan)dr["StartTime"];

                    model.EndTime = (TimeSpan)dr["EndTime"];

                    model.Venue = dr["Venue"].ToString();

                    model.Organizer = dr["Organizer"] == DBNull.Value
                        ? ""
                        : dr["Organizer"].ToString();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);

                    model.RegistrationFee = Convert.ToDecimal(dr["RegistrationFee"]);

                    model.MaxParticipants = Convert.ToInt32(dr["MaxParticipants"]);

                    model.AvailableSeats = Convert.ToInt32(dr["AvailableSeats"]);

                    model.ContactNumber = dr["ContactNumber"].ToString();

                    model.Email = dr["Email"] == DBNull.Value
                        ? ""
                        : dr["Email"].ToString();

                    model.Benefits = dr["Benefits"] == DBNull.Value
                        ? ""
                        : dr["Benefits"].ToString();

                    model.Instructions = dr["Instructions"] == DBNull.Value
                        ? ""
                        : dr["Instructions"].ToString();

                    model.CampImage = dr["CampImage"] == DBNull.Value
                        ? ""
                        : dr["CampImage"].ToString();

                    model.CampBanner = dr["CampBanner"] == DBNull.Value
                        ? ""
                        : dr["CampBanner"].ToString();

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }
                else
                {
                    TempData["Error"] = "Medical Camp not found.";

                    dr.Close();

                    return RedirectToAction("ManageMedicalCamp");
                }

                dr.Close();
            }

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMedicalCamp(MedicalCampModel model)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            model.DoctorList = new List<SelectListItem>();
            model.DepartmentList = new List<SelectListItem>();


            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand doctorCmd = new SqlCommand(@"
            SELECT
                DoctorId,
                DoctorName
            FROM tbl_Doctor
            WHERE IsActive = 1
            AND IsDeleted = 0
            ORDER BY DoctorName ASC", con);

                SqlDataReader doctorDr = doctorCmd.ExecuteReader();

                while (doctorDr.Read())
                {
                    model.DoctorList.Add(new SelectListItem
                    {
                        Value = doctorDr["DoctorId"].ToString(),
                        Text = doctorDr["DoctorName"].ToString()
                    });
                }

                doctorDr.Close();

                
                SqlCommand deptCmd = new SqlCommand(@"
            SELECT
                DepartmentId,
                DepartmentName
            FROM tbl_Department
            WHERE IsActive = 1
            AND IsDeleted = 0
            ORDER BY DepartmentName ASC", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                while (deptDr.Read())
                {
                    model.DepartmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }


            ModelState.Remove(nameof(MedicalCampModel.DoctorName));
            ModelState.Remove(nameof(MedicalCampModel.DepartmentName));
            ModelState.Remove(nameof(MedicalCampModel.CampImage));
            ModelState.Remove(nameof(MedicalCampModel.CampBanner));
            ModelState.Remove(nameof(MedicalCampModel.AvailableSeats));
            ModelState.Remove(nameof(MedicalCampModel.DoctorList));
            ModelState.Remove(nameof(MedicalCampModel.DepartmentList));


            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    if (model.CampImageFile != null &&
                        model.CampImageFile.Length > 0)
                    {
                        string folderPath =
                            Path.Combine(env.WebRootPath, "MedicalCamp");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.CampImageFile.FileName);

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        using (FileStream fs =
                            new FileStream(filePath, FileMode.Create))
                        {
                            model.CampImageFile.CopyTo(fs);
                        }

                      
                        model.CampImage = fileName;
                    }


                    if (model.CampBannerFile != null &&
                        model.CampBannerFile.Length > 0)
                    {
                        string folderPath =
                            Path.Combine(env.WebRootPath, "MedicalCamp");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString() +
                            Path.GetExtension(model.CampBannerFile.FileName);

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        using (FileStream fs =
                            new FileStream(filePath, FileMode.Create))
                        {
                            model.CampBannerFile.CopyTo(fs);
                        }

                        model.CampBanner = fileName;
                    }
                   

                    string oldCampImage = "";
                    string oldCampBanner = "";
                    int oldAvailableSeats = 0;
                    int oldMaxParticipants = 0;

                    SqlCommand oldCmd = new SqlCommand(@"
                SELECT
                    CampImage,
                    CampBanner,
                    AvailableSeats,
                    MaxParticipants
                FROM tbl_MedicalCamp
                WHERE CampId = @CampId
                AND IsDeleted = 0", con);

                    oldCmd.Parameters.AddWithValue("@CampId", model.CampId);

                    SqlDataReader oldDr = oldCmd.ExecuteReader();

                    if (oldDr.Read())
                    {
                        oldCampImage = oldDr["CampImage"] == DBNull.Value
                            ? ""
                            : oldDr["CampImage"].ToString();

                        oldCampBanner = oldDr["CampBanner"] == DBNull.Value
                            ? ""
                            : oldDr["CampBanner"].ToString();

                        oldAvailableSeats = Convert.ToInt32(
                            oldDr["AvailableSeats"]);

                        oldMaxParticipants = Convert.ToInt32(
                            oldDr["MaxParticipants"]);
                    }
                    else
                    {
                        oldDr.Close();

                        TempData["Error"] = "Medical Camp not found.";

                        return RedirectToAction("ManageMedicalCamp");
                    }

                    oldDr.Close();


                    if (string.IsNullOrWhiteSpace(model.CampImage))
                    {
                        model.CampImage = oldCampImage;
                    }
                    else
                    {
                       
                        if (!string.IsNullOrWhiteSpace(oldCampImage))
                        {
                            string oldImagePath = Path.Combine(
                                env.WebRootPath,
                                "MedicalCamp",
                                oldCampImage);

                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }


                    if (string.IsNullOrWhiteSpace(model.CampBanner))
                    {
                        model.CampBanner = oldCampBanner;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(oldCampBanner))
                        {
                            string oldBannerPath = Path.Combine(
                                env.WebRootPath,
                                "MedicalCamp",
                                oldCampBanner);

                            if (System.IO.File.Exists(oldBannerPath))
                            {
                                System.IO.File.Delete(oldBannerPath);
                            }
                        }
                    }

                    SqlCommand duplicateCmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM tbl_MedicalCamp
                WHERE CampTitle = @CampTitle
                AND CampDate = @CampDate
                AND CampId <> @CampId
                AND IsDeleted = 0", con);

                    duplicateCmd.Parameters.AddWithValue(
                        "@CampTitle",
                        model.CampTitle);

                    duplicateCmd.Parameters.AddWithValue(
                        "@CampDate",
                        model.CampDate);

                    duplicateCmd.Parameters.AddWithValue(
                        "@CampId",
                        model.CampId);

                    int duplicateCount =
                        Convert.ToInt32(
                            duplicateCmd.ExecuteScalar());

                    if (duplicateCount > 0)
                    {
                        TempData["Error"] =
                            "Another medical camp with the same title already exists on this date.";

                        return View(model);
                    }


                    int registeredParticipants =
                        oldMaxParticipants - oldAvailableSeats;

                    if (registeredParticipants < 0)
                    {
                        registeredParticipants = 0;
                    }

                    if (model.MaxParticipants < registeredParticipants)
                    {
                        TempData["Error"] =
                            "Maximum participants cannot be less than already registered participants.";

                        return View(model);
                    }

                    model.AvailableSeats =
                        model.MaxParticipants - registeredParticipants;

                    string updateQuery = @"
                UPDATE tbl_MedicalCamp
                SET

                    CampTitle = @CampTitle,

                    CampImage = @CampImage,

                    CampBanner = @CampBanner,

                    CampDescription = @CampDescription,

                    CampDate = @CampDate,

                    StartTime = @StartTime,

                    EndTime = @EndTime,

                    Venue = @Venue,

                    Organizer = @Organizer,

                    DoctorId = @DoctorId,

                    DepartmentId = @DepartmentId,

                    RegistrationFee = @RegistrationFee,

                    MaxParticipants = @MaxParticipants,

                    AvailableSeats = @AvailableSeats,

                    ContactNumber = @ContactNumber,

                    Email = @Email,

                    Benefits = @Benefits,

                    Instructions = @Instructions,

                    IsFeatured = @IsFeatured,

                    IsActive = @IsActive,

                    UpdatedDate = @UpdatedDate

                WHERE CampId = @CampId
                AND IsDeleted = 0";

                    SqlCommand cmd =
                        new SqlCommand(updateQuery, con);


                    cmd.Parameters.AddWithValue(
                        "@CampTitle",
                        model.CampTitle);

                    cmd.Parameters.AddWithValue(
                        "@CampImage",
                        string.IsNullOrWhiteSpace(model.CampImage)
                            ? DBNull.Value
                            : (object)model.CampImage);

                    cmd.Parameters.AddWithValue(
                        "@CampBanner",
                        string.IsNullOrWhiteSpace(model.CampBanner)
                            ? DBNull.Value
                            : (object)model.CampBanner);

                    cmd.Parameters.AddWithValue(
                        "@CampDescription",
                        model.CampDescription);

                    cmd.Parameters.AddWithValue(
                        "@CampDate",
                        model.CampDate);

                    cmd.Parameters.AddWithValue(
                        "@StartTime",
                        model.StartTime);

                    cmd.Parameters.AddWithValue(
                        "@EndTime",
                        model.EndTime);

                    cmd.Parameters.AddWithValue(
                        "@Venue",
                        model.Venue);

                    cmd.Parameters.AddWithValue(
                        "@Organizer",
                        string.IsNullOrWhiteSpace(model.Organizer)
                            ? DBNull.Value
                            : (object)model.Organizer);

                    cmd.Parameters.AddWithValue(
                        "@DoctorId",
                        model.DoctorId);

                    cmd.Parameters.AddWithValue(
                        "@DepartmentId",
                        model.DepartmentId);

                    cmd.Parameters.AddWithValue(
                        "@RegistrationFee",
                        model.RegistrationFee);

                    cmd.Parameters.AddWithValue(
                        "@MaxParticipants",
                        model.MaxParticipants);

                    cmd.Parameters.AddWithValue(
                        "@AvailableSeats",
                        model.AvailableSeats);

                    cmd.Parameters.AddWithValue(
                        "@ContactNumber",
                        model.ContactNumber);

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        string.IsNullOrWhiteSpace(model.Email)
                            ? DBNull.Value
                            : (object)model.Email);

                    cmd.Parameters.AddWithValue(
                        "@Benefits",
                        string.IsNullOrWhiteSpace(model.Benefits)
                            ? DBNull.Value
                            : (object)model.Benefits);

                    cmd.Parameters.AddWithValue(
                        "@Instructions",
                        string.IsNullOrWhiteSpace(model.Instructions)
                            ? DBNull.Value
                            : (object)model.Instructions);

                    cmd.Parameters.AddWithValue(
                        "@IsFeatured",
                        model.IsFeatured);

                    cmd.Parameters.AddWithValue(
                        "@IsActive",
                        model.IsActive);

                    cmd.Parameters.AddWithValue(
                        "@UpdatedDate",
                        DateTime.Now);

                    cmd.Parameters.AddWithValue(
                        "@CampId",
                        model.CampId);
                

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] =
                            "Medical Camp updated successfully.";

                        return RedirectToAction("ManageMedicalCamp");
                    }
                    else
                    {
                        TempData["Error"] =
                            "Unable to update medical camp.";

                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {

                TempData["Error"] = ex.Message;


                model.DoctorList = new List<SelectListItem>();

                model.DepartmentList = new List<SelectListItem>();

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand doctorCmd = new SqlCommand(@"
                SELECT
                    DoctorId,
                    DoctorName
                FROM tbl_Doctor
                WHERE IsActive = 1
                AND IsDeleted = 0
                ORDER BY DoctorName ASC", con);

                    SqlDataReader doctorDr =
                        doctorCmd.ExecuteReader();

                    while (doctorDr.Read())
                    {
                        model.DoctorList.Add(new SelectListItem
                        {
                            Value = doctorDr["DoctorId"].ToString(),
                            Text = doctorDr["DoctorName"].ToString()
                        });
                    }

                    doctorDr.Close();


                    SqlCommand deptCmd = new SqlCommand(@"
                SELECT
                    DepartmentId,
                    DepartmentName
                FROM tbl_Department
                WHERE IsActive = 1
                AND IsDeleted = 0
                ORDER BY DepartmentName ASC", con);

                    SqlDataReader deptDr =
                        deptCmd.ExecuteReader();

                    while (deptDr.Read())
                    {
                        model.DepartmentList.Add(new SelectListItem
                        {
                            Value = deptDr["DepartmentId"].ToString(),
                            Text = deptDr["DepartmentName"].ToString()
                        });
                    }

                    deptDr.Close();
                }

                return View(model);
            }
        }
        [HttpGet]
        public IActionResult ViewMedicalCamp(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            MedicalCampModel model = new MedicalCampModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string query = @"
            SELECT
                mc.CampId,
                mc.CampTitle,
                mc.CampImage,
                mc.CampBanner,
                mc.CampDescription,
                mc.CampDate,
                mc.StartTime,
                mc.EndTime,
                mc.Venue,
                mc.Organizer,
                mc.DoctorId,
                d.DoctorName,
                mc.DepartmentId,
                dep.DepartmentName,
                mc.RegistrationFee,
                mc.MaxParticipants,
                mc.AvailableSeats,
                mc.ContactNumber,
                mc.Email,
                mc.Benefits,
                mc.Instructions,
                mc.IsFeatured,
                mc.IsActive,
                mc.IsDeleted,
                mc.CreatedDate,
                mc.UpdatedDate

            FROM tbl_MedicalCamp mc

            INNER JOIN tbl_Doctor d
                ON mc.DoctorId = d.DoctorId

            INNER JOIN tbl_Department dep
                ON mc.DepartmentId = dep.DepartmentId

            WHERE
                mc.CampId = @CampId
                AND mc.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CampId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {

                    model.CampId =
                        Convert.ToInt64(dr["CampId"]);

                    model.CampTitle =
                        dr["CampTitle"].ToString();

                    model.CampDescription =
                        dr["CampDescription"].ToString();


                    model.CampImage =
                        dr["CampImage"] == DBNull.Value
                        ? ""
                        : dr["CampImage"].ToString();

                    model.CampBanner =
                        dr["CampBanner"] == DBNull.Value
                        ? ""
                        : dr["CampBanner"].ToString();


                    if (dr["CampDate"] != DBNull.Value)
                    {
                        model.CampDate =
                            Convert.ToDateTime(dr["CampDate"]);
                    }

                    if (dr["StartTime"] != DBNull.Value)
                    {
                        model.StartTime =
                            (TimeSpan)dr["StartTime"];
                    }

                    if (dr["EndTime"] != DBNull.Value)
                    {
                        model.EndTime =
                            (TimeSpan)dr["EndTime"];
                    }


                    model.Venue =
                        dr["Venue"].ToString();

                    model.Organizer =
                        dr["Organizer"] == DBNull.Value
                        ? ""
                        : dr["Organizer"].ToString();


                    model.DoctorId =
                        Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName =
                        dr["DoctorName"].ToString();


                    model.DepartmentId =
                        Convert.ToInt64(dr["DepartmentId"]);

                    model.DepartmentName =
                        dr["DepartmentName"].ToString();


                    model.RegistrationFee =
                        Convert.ToDecimal(dr["RegistrationFee"]);

                    model.MaxParticipants =
                        Convert.ToInt32(dr["MaxParticipants"]);

                    model.AvailableSeats =
                        Convert.ToInt32(dr["AvailableSeats"]);


                    model.ContactNumber =
                        dr["ContactNumber"].ToString();

                    model.Email =
                        dr["Email"] == DBNull.Value
                        ? ""
                        : dr["Email"].ToString();


                    model.Benefits =
                        dr["Benefits"] == DBNull.Value
                        ? ""
                        : dr["Benefits"].ToString();

                    model.Instructions =
                        dr["Instructions"] == DBNull.Value
                        ? ""
                        : dr["Instructions"].ToString();


                    model.IsFeatured =
                        Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive =
                        Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted =
                        Convert.ToBoolean(dr["IsDeleted"]);


                    if (dr["CreatedDate"] != DBNull.Value)
                    {
                        model.CreatedDate =
                            Convert.ToDateTime(dr["CreatedDate"]);
                    }

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate =
                            Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] =
                        "Medical Camp not found.";

                    return RedirectToAction("ManageMedicalCamp");
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ChangeCampStatus(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
                SELECT IsActive
                FROM tbl_MedicalCamp
                WHERE CampId = @CampId
                AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@CampId", id);

                    object currentStatus =
                        checkCmd.ExecuteScalar();


                    if (currentStatus == null)
                    {
                        TempData["Error"] =
                            "Medical Camp not found.";

                        return RedirectToAction("ManageMedicalCamp");
                    }

                    bool isActive =
                        Convert.ToBoolean(currentStatus);


                    bool newStatus = !isActive;

                    SqlCommand updateCmd = new SqlCommand(@"
                UPDATE tbl_MedicalCamp
                SET
                    IsActive = @IsActive,
                    UpdatedDate = @UpdatedDate
                WHERE CampId = @CampId
                AND IsDeleted = 0", con);

                    updateCmd.Parameters.AddWithValue(
                        "@IsActive",
                        newStatus);

                    updateCmd.Parameters.AddWithValue(
                        "@UpdatedDate",
                        DateTime.Now);

                    updateCmd.Parameters.AddWithValue(
                        "@CampId",
                        id);

                    int result =
                        updateCmd.ExecuteNonQuery();


                    if (result > 0)
                    {
                        if (newStatus)
                        {
                            TempData["Success"] =
                                "Medical Camp activated successfully.";
                        }
                        else
                        {
                            TempData["Success"] =
                                "Medical Camp deactivated successfully.";
                        }
                    }
                    else
                    {
                        TempData["Error"] =
                            "Unable to change medical camp status.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageMedicalCamp");
        }
        

       
        [HttpGet]
        public IActionResult DeleteMedicalCamp(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
                SELECT CampId
                FROM tbl_MedicalCamp
                WHERE CampId = @CampId
                AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@CampId", id);

                    object campId = checkCmd.ExecuteScalar();

                    if (campId == null)
                    {
                        TempData["Error"] =
                            "Medical Camp not found or already deleted.";

                        return RedirectToAction("ManageMedicalCamp");
                    }

                    SqlCommand deleteCmd = new SqlCommand(@"
                UPDATE tbl_MedicalCamp
                SET
                    IsDeleted = 1,
                    IsActive = 0,
                    IsFeatured = 0,
                    UpdatedDate = @UpdatedDate
                WHERE CampId = @CampId
                AND IsDeleted = 0", con);

                    deleteCmd.Parameters.AddWithValue(
                        "@UpdatedDate",
                        DateTime.Now);

                    deleteCmd.Parameters.AddWithValue(
                        "@CampId",
                        id);

                    int result = deleteCmd.ExecuteNonQuery();


                    if (result > 0)
                    {
                        TempData["Success"] =
                            "Medical Camp deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] =
                            "Unable to delete Medical Camp.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageMedicalCamp");
        }

        [HttpGet]
        public IActionResult ManageCampRegistration()
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            List<CampRegistrationModel> list =
                new List<CampRegistrationModel>();


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    string query = @"
SELECT

    -- Registration
    cr.CampRegistrationId,
    cr.CampId,
    cr.CustomerId,

    cr.RegistrationNo,
    cr.RegistrationDate,

    cr.ParticipantName,
    cr.MobileNo,
    cr.Email,
    cr.Age,
    cr.Gender,
    cr.HealthConcern,

    cr.RegistrationStatus,
    cr.PaymentStatus,
    cr.Amount,
    cr.AdminRemark,

    cr.IsActive,
    cr.IsDeleted,

    cr.CreatedDate,
    cr.UpdatedDate,


    -- Customer
    c.FullName AS CustomerName,


    -- Medical Camp
    mc.CampTitle,
    mc.CampImage,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,
    mc.Organizer,
    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats,


    -- Doctor
    d.DoctorName,


    -- Department
    dep.DepartmentName


FROM tbl_CampRegistration cr


INNER JOIN tbl_Customer c
    ON cr.CustomerId = c.CustomerId


INNER JOIN tbl_MedicalCamp mc
    ON cr.CampId = mc.CampId


LEFT JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId


LEFT JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId


WHERE
    cr.IsDeleted = 0


ORDER BY
    cr.RegistrationDate DESC";


                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                CampRegistrationModel model =
                                    new CampRegistrationModel();


                                model.CampRegistrationId =
                                    Convert.ToInt64(
                                        dr["CampRegistrationId"]);


                                model.CampId =
                                    Convert.ToInt64(
                                        dr["CampId"]);


                                model.CustomerId =
                                    Convert.ToInt64(
                                        dr["CustomerId"]);


                                model.RegistrationNo =
                                    dr["RegistrationNo"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["RegistrationNo"].ToString();


                                if (dr["RegistrationDate"] !=
                                    DBNull.Value)
                                {
                                    model.RegistrationDate =
                                        Convert.ToDateTime(
                                            dr["RegistrationDate"]);
                                }


                                model.ParticipantName =
                                    dr["ParticipantName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["ParticipantName"].ToString();


                                model.MobileNo =
                                    dr["MobileNo"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["MobileNo"].ToString();


                                model.Email =
                                    dr["Email"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Email"].ToString();


                                if (dr["Age"] != DBNull.Value)
                                {
                                    model.Age =
                                        Convert.ToInt32(
                                            dr["Age"]);
                                }


                                model.Gender =
                                    dr["Gender"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Gender"].ToString();


                                model.HealthConcern =
                                    dr["HealthConcern"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["HealthConcern"].ToString();



                                model.RegistrationStatus =
                                    dr["RegistrationStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["RegistrationStatus"].ToString();


                                model.PaymentStatus =
                                    dr["PaymentStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["PaymentStatus"].ToString();


                                model.Amount =
                                    dr["Amount"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["Amount"]);


                                model.AdminRemark =
                                    dr["AdminRemark"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["AdminRemark"].ToString();



                                model.CustomerName =
                                    dr["CustomerName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CustomerName"].ToString();


                                model.CampTitle =
                                    dr["CampTitle"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampTitle"].ToString();


                                model.CampImage =
                                    dr["CampImage"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampImage"].ToString();


                                if (dr["CampDate"] !=
                                    DBNull.Value)
                                {
                                    model.CampDate =
                                        Convert.ToDateTime(
                                            dr["CampDate"]);
                                }


                                if (dr["StartTime"] !=
                                    DBNull.Value)
                                {
                                    model.StartTime =
                                        (TimeSpan)
                                        dr["StartTime"];
                                }


                                if (dr["EndTime"] !=
                                    DBNull.Value)
                                {
                                    model.EndTime =
                                        (TimeSpan)
                                        dr["EndTime"];
                                }


                                model.Venue =
                                    dr["Venue"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Venue"].ToString();


                                model.Organizer =
                                    dr["Organizer"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Organizer"].ToString();



                                model.RegistrationFee =
                                    dr["RegistrationFee"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["RegistrationFee"]);


                                model.MaxParticipants =
                                    dr["MaxParticipants"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["MaxParticipants"]);


                                model.AvailableSeats =
                                    dr["AvailableSeats"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["AvailableSeats"]);


                                model.DoctorName =
                                    dr["DoctorName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DoctorName"].ToString();



                                model.DepartmentName =
                                    dr["DepartmentName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DepartmentName"].ToString();


                                model.IsActive =
                                    dr["IsActive"] ==
                                    DBNull.Value
                                    ? false
                                    : Convert.ToBoolean(
                                        dr["IsActive"]);


                                model.IsDeleted =
                                    dr["IsDeleted"] ==
                                    DBNull.Value
                                    ? false
                                    : Convert.ToBoolean(
                                        dr["IsDeleted"]);


                                if (dr["CreatedDate"] !=
                                    DBNull.Value)
                                {
                                    model.CreatedDate =
                                        Convert.ToDateTime(
                                            dr["CreatedDate"]);
                                }


                                if (dr["UpdatedDate"] !=
                                    DBNull.Value)
                                {
                                    model.UpdatedDate =
                                        Convert.ToDateTime(
                                            dr["UpdatedDate"]);
                                }


                                list.Add(model);
                            }
                        }
                    }
                }


                return View(list);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load camp registrations: "
                    + ex.Message;


                return View(list);
            }
        }

        [HttpGet]
        public IActionResult ViewCampRegistration(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            CampRegistrationModel model =
                new CampRegistrationModel();


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();



                    string query = @"
SELECT

    -- Registration
    cr.CampRegistrationId,
    cr.CampId,
    cr.CustomerId,

    cr.RegistrationNo,
    cr.RegistrationDate,

    cr.ParticipantName,
    cr.MobileNo,
    cr.Email,
    cr.Age,
    cr.Gender,
    cr.HealthConcern,

    cr.RegistrationStatus,
    cr.PaymentStatus,
    cr.Amount,
    cr.AdminRemark,

    cr.IsActive,
    cr.IsDeleted,

    cr.CreatedDate,
    cr.UpdatedDate,


    -- Customer
    c.FullName AS CustomerName,


    -- Medical Camp
    mc.CampTitle,
    mc.CampImage,
    mc.CampBanner,
    mc.CampDescription,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,
    mc.Organizer,
    mc.Benefits,
    mc.Instructions,
    mc.ContactNumber,
    mc.Email AS CampEmail,

    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats,


    -- Doctor
    d.DoctorName,


    -- Department
    dep.DepartmentName


FROM tbl_CampRegistration cr


INNER JOIN tbl_Customer c
    ON cr.CustomerId = c.CustomerId


INNER JOIN tbl_MedicalCamp mc
    ON cr.CampId = mc.CampId


LEFT JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId


LEFT JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId


WHERE
    cr.CampRegistrationId = @CampRegistrationId
    AND cr.IsDeleted = 0";


                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CampRegistrationId",
                            SqlDbType.BigInt).Value = id;


                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {

                            if (dr.Read())
                            {

                                model.CampRegistrationId =
                                    Convert.ToInt64(
                                        dr["CampRegistrationId"]);


                                model.CampId =
                                    Convert.ToInt64(
                                        dr["CampId"]);


                                model.CustomerId =
                                    Convert.ToInt64(
                                        dr["CustomerId"]);


                                model.RegistrationNo =
                                    dr["RegistrationNo"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["RegistrationNo"].ToString();


                                if (dr["RegistrationDate"] !=
                                    DBNull.Value)
                                {
                                    model.RegistrationDate =
                                        Convert.ToDateTime(
                                            dr["RegistrationDate"]);
                                }


                                model.ParticipantName =
                                    dr["ParticipantName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["ParticipantName"].ToString();


                                model.MobileNo =
                                    dr["MobileNo"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["MobileNo"].ToString();


                                model.Email =
                                    dr["Email"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Email"].ToString();


                                if (dr["Age"] != DBNull.Value)
                                {
                                    model.Age =
                                        Convert.ToInt32(
                                            dr["Age"]);
                                }


                                model.Gender =
                                    dr["Gender"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Gender"].ToString();


                                model.HealthConcern =
                                    dr["HealthConcern"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["HealthConcern"].ToString();


                                model.RegistrationStatus =
                                    dr["RegistrationStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["RegistrationStatus"].ToString();


                                model.PaymentStatus =
                                    dr["PaymentStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["PaymentStatus"].ToString();


                                model.Amount =
                                    dr["Amount"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["Amount"]);


                                model.AdminRemark =
                                    dr["AdminRemark"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["AdminRemark"].ToString();


                                model.CustomerName =
                                    dr["CustomerName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CustomerName"].ToString();


                                model.CampTitle =
                                    dr["CampTitle"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampTitle"].ToString();


                                model.CampImage =
                                    dr["CampImage"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampImage"].ToString();


                                model.CampBanner =
                                    dr["CampBanner"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampBanner"].ToString();


                                model.CampDescription =
                                    dr["CampDescription"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampDescription"].ToString();


                                if (dr["CampDate"] !=
                                    DBNull.Value)
                                {
                                    model.CampDate =
                                        Convert.ToDateTime(
                                            dr["CampDate"]);
                                }


                                if (dr["StartTime"] !=
                                    DBNull.Value)
                                {
                                    model.StartTime =
                                        (TimeSpan)
                                        dr["StartTime"];
                                }


                                if (dr["EndTime"] !=
                                    DBNull.Value)
                                {
                                    model.EndTime =
                                        (TimeSpan)
                                        dr["EndTime"];
                                }



                                model.Venue =
                                    dr["Venue"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Venue"].ToString();


                                model.Organizer =
                                    dr["Organizer"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Organizer"].ToString();


                                model.Benefits =
                                    dr["Benefits"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Benefits"].ToString();


                                model.Instructions =
                                    dr["Instructions"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Instructions"].ToString();


                                model.ContactNumber =
                                    dr["ContactNumber"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["ContactNumber"].ToString();


                                model.CampEmail =
                                    dr["CampEmail"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampEmail"].ToString();


                                model.RegistrationFee =
                                    dr["RegistrationFee"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["RegistrationFee"]);


                                model.MaxParticipants =
                                    dr["MaxParticipants"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["MaxParticipants"]);


                                model.AvailableSeats =
                                    dr["AvailableSeats"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["AvailableSeats"]);



                                model.DoctorName =
                                    dr["DoctorName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DoctorName"].ToString();


                                model.DepartmentName =
                                    dr["DepartmentName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DepartmentName"].ToString();



                                model.IsActive =
                                    dr["IsActive"] ==
                                    DBNull.Value
                                    ? false
                                    : Convert.ToBoolean(
                                        dr["IsActive"]);


                                model.IsDeleted =
                                    dr["IsDeleted"] ==
                                    DBNull.Value
                                    ? false
                                    : Convert.ToBoolean(
                                        dr["IsDeleted"]);


                                if (dr["CreatedDate"] !=
                                    DBNull.Value)
                                {
                                    model.CreatedDate =
                                        Convert.ToDateTime(
                                            dr["CreatedDate"]);
                                }


                                if (dr["UpdatedDate"] !=
                                    DBNull.Value)
                                {
                                    model.UpdatedDate =
                                        Convert.ToDateTime(
                                            dr["UpdatedDate"]);
                                }
                            }
                            else
                            {
                                TempData["Error"] =
                                    "Camp registration not found.";

                                return RedirectToAction(
                                    "ManageCampRegistration");
                            }
                        }
                    }
                }


                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load camp registration details: "
                    + ex.Message;

                return RedirectToAction(
                    "ManageCampRegistration");
            }
        }

        [HttpGet]
        public IActionResult ChangeCampRegistrationStatus(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            CampRegistrationModel model =
                new CampRegistrationModel();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                SELECT
                    cr.CampRegistrationId,
                    cr.CampId,
                    cr.CustomerId,
                    cr.RegistrationNo,
                    cr.RegistrationDate,
                    cr.ParticipantName,
                    cr.MobileNo,
                    cr.Email,
                    cr.Age,
                    cr.Gender,
                    cr.HealthConcern,
                    cr.RegistrationStatus,
                    cr.PaymentStatus,
                    cr.Amount,
                    cr.AdminRemark,
                    cr.IsActive,
                    cr.IsDeleted,
                    cr.CreatedDate,
                    cr.UpdatedDate,

                    c.CustomerName,

                    mc.CampTitle,
                    mc.CampDate,
                    mc.StartTime,
                    mc.EndTime,
                    mc.Venue,
                    mc.Organizer

                FROM tbl_CampRegistration cr

                INNER JOIN tbl_Customer c
                    ON cr.CustomerId = c.CustomerId

                INNER JOIN tbl_MedicalCamp mc
                    ON cr.CampId = mc.CampId

                WHERE
                    cr.CampRegistrationId = @CampRegistrationId
                    AND cr.IsDeleted = 0";


                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CampRegistrationId",
                            SqlDbType.BigInt).Value = id;


                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                

                                model.CampRegistrationId =
                                    Convert.ToInt64(
                                        dr["CampRegistrationId"]);

                                model.CampId =
                                    Convert.ToInt64(
                                        dr["CampId"]);

                                model.CustomerId =
                                    Convert.ToInt64(
                                        dr["CustomerId"]);


                                model.RegistrationNo =
                                    dr["RegistrationNo"] == DBNull.Value
                                        ? ""
                                        : dr["RegistrationNo"].ToString();


                                if (dr["RegistrationDate"] != DBNull.Value)
                                {
                                    model.RegistrationDate =
                                        Convert.ToDateTime(
                                            dr["RegistrationDate"]);
                                }

                                model.ParticipantName =
                                    dr["ParticipantName"] == DBNull.Value
                                        ? ""
                                        : dr["ParticipantName"].ToString();


                                model.MobileNo =
                                    dr["MobileNo"] == DBNull.Value
                                        ? ""
                                        : dr["MobileNo"].ToString();


                                model.Email =
                                    dr["Email"] == DBNull.Value
                                        ? ""
                                        : dr["Email"].ToString();


                                if (dr["Age"] != DBNull.Value)
                                {
                                    model.Age =
                                        Convert.ToInt32(
                                            dr["Age"]);
                                }


                                model.Gender =
                                    dr["Gender"] == DBNull.Value
                                        ? ""
                                        : dr["Gender"].ToString();


                                model.HealthConcern =
                                    dr["HealthConcern"] == DBNull.Value
                                        ? ""
                                        : dr["HealthConcern"].ToString();


                                model.RegistrationStatus =
                                    dr["RegistrationStatus"] == DBNull.Value
                                        ? "Pending"
                                        : dr["RegistrationStatus"].ToString();


                                model.PaymentStatus =
                                    dr["PaymentStatus"] == DBNull.Value
                                        ? ""
                                        : dr["PaymentStatus"].ToString();


                                model.Amount =
                                    dr["Amount"] == DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(
                                            dr["Amount"]);


                                model.AdminRemark =
                                    dr["AdminRemark"] == DBNull.Value
                                        ? ""
                                        : dr["AdminRemark"].ToString();


                                model.CustomerName =
                                    dr["CustomerName"] == DBNull.Value
                                        ? ""
                                        : dr["CustomerName"].ToString();


                                model.CampTitle =
                                    dr["CampTitle"] == DBNull.Value
                                        ? ""
                                        : dr["CampTitle"].ToString();


                                if (dr["CampDate"] != DBNull.Value)
                                {
                                    model.CampDate =
                                        Convert.ToDateTime(
                                            dr["CampDate"]);
                                }


                                if (dr["StartTime"] != DBNull.Value)
                                {
                                    model.StartTime =
                                        (TimeSpan)dr["StartTime"];
                                }


                                if (dr["EndTime"] != DBNull.Value)
                                {
                                    model.EndTime =
                                        (TimeSpan)dr["EndTime"];
                                }


                                model.Venue =
                                    dr["Venue"] == DBNull.Value
                                        ? ""
                                        : dr["Venue"].ToString();


                                model.Organizer =
                                    dr["Organizer"] == DBNull.Value
                                        ? ""
                                        : dr["Organizer"].ToString();



                                model.IsActive =
                                    dr["IsActive"] == DBNull.Value
                                        ? false
                                        : Convert.ToBoolean(
                                            dr["IsActive"]);


                                model.IsDeleted =
                                    dr["IsDeleted"] == DBNull.Value
                                        ? false
                                        : Convert.ToBoolean(
                                            dr["IsDeleted"]);


                                if (dr["CreatedDate"] != DBNull.Value)
                                {
                                    model.CreatedDate =
                                        Convert.ToDateTime(
                                            dr["CreatedDate"]);
                                }


                                if (dr["UpdatedDate"] != DBNull.Value)
                                {
                                    model.UpdatedDate =
                                        Convert.ToDateTime(
                                            dr["UpdatedDate"]);
                                }
                            }
                            else
                            {
                                TempData["Error"] =
                                    "Camp registration not found.";

                                return RedirectToAction(
                                    "ManageCampRegistration");
                            }
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load registration status details: "
                    + ex.Message;

                return RedirectToAction(
                    "ManageCampRegistration");
            }
        }
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeCampRegistrationStatus(
            CampRegistrationModel model)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (model.CampRegistrationId <= 0)
            {
                TempData["Error"] =
                    "Invalid camp registration.";

                return RedirectToAction(
                    "ManageCampRegistration");
            }


            if (string.IsNullOrWhiteSpace(
                model.RegistrationStatus))
            {
                TempData["Error"] =
                    "Please select registration status.";

                return RedirectToAction(
                    "ChangeCampRegistrationStatus",
                    new
                    {
                        id = model.CampRegistrationId
                    });
            }

            string[] allowedStatuses =
            {
        "Pending",
        "Confirmed",
        "Completed",
        "Cancelled"
    };


            if (!allowedStatuses.Contains(
                model.RegistrationStatus))
            {
                TempData["Error"] =
                    "Invalid registration status.";

                return RedirectToAction(
                    "ChangeCampRegistrationStatus",
                    new
                    {
                        id = model.CampRegistrationId
                    });
            }


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    string checkQuery = @"
                SELECT COUNT(*)
                FROM tbl_CampRegistration
                WHERE CampRegistrationId = @CampRegistrationId
                AND IsDeleted = 0";


                    using (SqlCommand checkCmd =
                        new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.Add(
                            "@CampRegistrationId",
                            SqlDbType.BigInt).Value =
                            model.CampRegistrationId;


                        int count =
                            Convert.ToInt32(
                                checkCmd.ExecuteScalar());


                        if (count == 0)
                        {
                            TempData["Error"] =
                                "Camp registration not found.";

                            return RedirectToAction(
                                "ManageCampRegistration");
                        }
                    }

                    string updateQuery = @"
                UPDATE tbl_CampRegistration
                SET
                    RegistrationStatus = @RegistrationStatus,
                    AdminRemark = @AdminRemark,
                    UpdatedDate = GETDATE()
                WHERE
                    CampRegistrationId = @CampRegistrationId
                    AND IsDeleted = 0";


                    using (SqlCommand cmd =
                        new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.Add(
                            "@RegistrationStatus",
                            SqlDbType.NVarChar, 50).Value =
                            model.RegistrationStatus.Trim();


                        cmd.Parameters.Add(
                            "@AdminRemark",
                            SqlDbType.NVarChar, -1).Value =
                            string.IsNullOrWhiteSpace(
                                model.AdminRemark)
                            ? (object)DBNull.Value
                            : model.AdminRemark.Trim();


                        cmd.Parameters.Add(
                            "@CampRegistrationId",
                            SqlDbType.BigInt).Value =
                            model.CampRegistrationId;


                        int rowsAffected =
                            cmd.ExecuteNonQuery();


                        if (rowsAffected > 0)
                        {
                            TempData["Success"] =
                                "Registration status changed to "
                                + model.RegistrationStatus
                                + " successfully.";
                        }
                        else
                        {
                            TempData["Error"] =
                                "Unable to update registration status.";
                        }
                    }
                }

                return RedirectToAction(
                    "ViewCampRegistration",
                    new
                    {
                        id = model.CampRegistrationId
                    });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to change registration status: "
                    + ex.Message;

                return RedirectToAction(
                    "ViewCampRegistration",
                    new
                    {
                        id = model.CampRegistrationId
                    });
            }
        }
        
        [HttpGet]
        public IActionResult PrintCampRegistration(long id)
        {

            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            CampRegistrationModel model =
                new CampRegistrationModel();


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    string query = @"
SELECT

    -- Registration
    cr.CampRegistrationId,
    cr.CampId,
    cr.CustomerId,
    cr.RegistrationNo,
    cr.RegistrationDate,
    cr.ParticipantName,
    cr.MobileNo,
    cr.Email,
    cr.Age,
    cr.Gender,
    cr.HealthConcern,
    cr.RegistrationStatus,
    cr.PaymentStatus,
    cr.Amount,
    cr.AdminRemark,
    cr.IsActive,
    cr.IsDeleted,
    cr.CreatedDate,
    cr.UpdatedDate,

    -- Customer
    c.FullName AS CustomerName,

    -- Camp
    mc.CampTitle,
    mc.CampImage,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,
    mc.Organizer,
    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats,

    -- Doctor
    d.DoctorName,

    -- Department
    dep.DepartmentName

FROM tbl_CampRegistration cr

INNER JOIN tbl_Customer c
    ON cr.CustomerId = c.CustomerId

INNER JOIN tbl_MedicalCamp mc
    ON cr.CampId = mc.CampId

LEFT JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId

LEFT JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId

WHERE
    cr.CampRegistrationId = @CampRegistrationId
    AND cr.IsDeleted = 0";


                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CampRegistrationId",
                            SqlDbType.BigInt).Value = id;


                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Camp registration not found.";

                                return RedirectToAction(
                                    "ManageCampRegistration");
                            }

                            model.CampRegistrationId =
                                Convert.ToInt64(
                                    dr["CampRegistrationId"]);


                            model.CampId =
                                Convert.ToInt64(
                                    dr["CampId"]);


                            model.CustomerId =
                                Convert.ToInt64(
                                    dr["CustomerId"]);


                            model.RegistrationNo =
                                dr["RegistrationNo"] ==
                                DBNull.Value
                                ? ""
                                : dr["RegistrationNo"].ToString();


                            if (dr["RegistrationDate"] !=
                                DBNull.Value)
                            {
                                model.RegistrationDate =
                                    Convert.ToDateTime(
                                        dr["RegistrationDate"]);
                            }

                            model.ParticipantName =
                                dr["ParticipantName"] ==
                                DBNull.Value
                                ? ""
                                : dr["ParticipantName"].ToString();


                            model.MobileNo =
                                dr["MobileNo"] ==
                                DBNull.Value
                                ? ""
                                : dr["MobileNo"].ToString();


                            model.Email =
                                dr["Email"] ==
                                DBNull.Value
                                ? ""
                                : dr["Email"].ToString();


                            if (dr["Age"] != DBNull.Value)
                            {
                                model.Age =
                                    Convert.ToInt32(
                                        dr["Age"]);
                            }


                            model.Gender =
                                dr["Gender"] ==
                                DBNull.Value
                                ? ""
                                : dr["Gender"].ToString();


                            model.HealthConcern =
                                dr["HealthConcern"] ==
                                DBNull.Value
                                ? ""
                                : dr["HealthConcern"].ToString();


                            model.RegistrationStatus =
                                dr["RegistrationStatus"] ==
                                DBNull.Value
                                ? ""
                                : dr["RegistrationStatus"].ToString();


                            model.PaymentStatus =
                                dr["PaymentStatus"] ==
                                DBNull.Value
                                ? ""
                                : dr["PaymentStatus"].ToString();


                            model.Amount =
                                dr["Amount"] ==
                                DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["Amount"]);


                            model.AdminRemark =
                                dr["AdminRemark"] ==
                                DBNull.Value
                                ? ""
                                : dr["AdminRemark"].ToString();


                            model.CustomerName =
                                dr["CustomerName"] ==
                                DBNull.Value
                                ? ""
                                : dr["CustomerName"].ToString();


                            model.CampTitle =
                                dr["CampTitle"] ==
                                DBNull.Value
                                ? ""
                                : dr["CampTitle"].ToString();


                            model.CampImage =
                                dr["CampImage"] ==
                                DBNull.Value
                                ? ""
                                : dr["CampImage"].ToString();


                            if (dr["CampDate"] !=
                                DBNull.Value)
                            {
                                model.CampDate =
                                    Convert.ToDateTime(
                                        dr["CampDate"]);
                            }

                            if (dr["StartTime"] !=
                                DBNull.Value)
                            {
                                model.StartTime =
                                    (TimeSpan)
                                    dr["StartTime"];
                            }


                            if (dr["EndTime"] !=
                                DBNull.Value)
                            {
                                model.EndTime =
                                    (TimeSpan)
                                    dr["EndTime"];
                            }

                            model.Venue =
                                dr["Venue"] ==
                                DBNull.Value
                                ? ""
                                : dr["Venue"].ToString();


                            model.Organizer =
                                dr["Organizer"] ==
                                DBNull.Value
                                ? ""
                                : dr["Organizer"].ToString();


                            model.RegistrationFee =
                                dr["RegistrationFee"] ==
                                DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["RegistrationFee"]);


                            model.MaxParticipants =
                                dr["MaxParticipants"] ==
                                DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    dr["MaxParticipants"]);


                            model.AvailableSeats =
                                dr["AvailableSeats"] ==
                                DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    dr["AvailableSeats"]);


                            model.DoctorName =
                                dr["DoctorName"] ==
                                DBNull.Value
                                ? ""
                                : dr["DoctorName"].ToString();


                            model.DepartmentName =
                                dr["DepartmentName"] ==
                                DBNull.Value
                                ? ""
                                : dr["DepartmentName"].ToString();


                            model.IsActive =
                                dr["IsActive"] ==
                                DBNull.Value
                                ? false
                                : Convert.ToBoolean(
                                    dr["IsActive"]);


                            model.IsDeleted =
                                dr["IsDeleted"] ==
                                DBNull.Value
                                ? false
                                : Convert.ToBoolean(
                                    dr["IsDeleted"]);



                            if (dr["CreatedDate"] !=
                                DBNull.Value)
                            {
                                model.CreatedDate =
                                    Convert.ToDateTime(
                                        dr["CreatedDate"]);
                            }


                            if (dr["UpdatedDate"] !=
                                DBNull.Value)
                            {
                                model.UpdatedDate =
                                    Convert.ToDateTime(
                                        dr["UpdatedDate"]);
                            }
                        }
                    }
                }


                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to print registration: "
                    + ex.Message;

                return RedirectToAction(
                    "ManageCampRegistration");
            }
        }
      

        [HttpGet]
        public IActionResult ManageLabTest(string search = "", string category = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<LabTestModel> list = new List<LabTestModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"SELECT * FROM tbl_LabTest
                                 WHERE IsDeleted = 0
                                 AND (@Search = '' OR TestName LIKE '%' + @Search + '%' OR Description LIKE '%' + @Search + '%')
                                 AND (@Category = '' OR TestCategory = @Category)
                                 ORDER BY LabTestId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@Category", category ?? "");

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabTestModel model = new LabTestModel
                    {
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        TestName = dr["TestName"].ToString(),
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "",
                        Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "",
                        SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "",
                        PreparationInstructions = dr["PreparationInstructions"] != DBNull.Value ? dr["PreparationInstructions"].ToString() : "",
                        ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "",
                        Price = Convert.ToDecimal(dr["Price"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        IsDeleted = Convert.ToBoolean(dr["IsDeleted"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null
                    };
                    list.Add(model);
                }
            }

            ViewBag.Search = search;
            ViewBag.Category = category;
            return View("~/Views/Admin/LabTest/ManageLabTest.cshtml", list);
        }

        [HttpGet]
        public IActionResult AddLabTest()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View("~/Views/Admin/LabTest/AddLabTest.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLabTest(LabTestModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/LabTest/AddLabTest.cshtml", model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM tbl_LabTest WHERE TestName=@TestName AND IsDeleted=0", con);
                    checkCmd.Parameters.AddWithValue("@TestName", model.TestName);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "A Lab Test with this name already exists.";
                        return View("~/Views/Admin/LabTest/AddLabTest.cshtml", model);
                    }

                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO tbl_LabTest (TestName, TestCategory, Description, SampleType, PreparationInstructions, ReportTime, Price, IsActive, IsDeleted, CreatedDate)
                        VALUES (@TestName, @TestCategory, @Description, @SampleType, @PreparationInstructions, @ReportTime, @Price, 1, 0, GETDATE())", con);

                    cmd.Parameters.AddWithValue("@TestName", model.TestName);
                    cmd.Parameters.AddWithValue("@TestCategory", (object)model.TestCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SampleType", (object)model.SampleType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreparationInstructions", (object)model.PreparationInstructions ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportTime", (object)model.ReportTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Price", model.Price);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Lab Test added successfully.";
                    return RedirectToAction("ManageLabTest");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Admin/LabTest/AddLabTest.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult EditLabTest(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabTestModel model = new LabTestModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_LabTest WHERE LabTestId=@LabTestId AND IsDeleted=0", con);
                cmd.Parameters.AddWithValue("@LabTestId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.TestName = dr["TestName"].ToString();
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "";
                    model.SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "";
                    model.PreparationInstructions = dr["PreparationInstructions"] != DBNull.Value ? dr["PreparationInstructions"].ToString() : "";
                    model.ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "";
                    model.Price = Convert.ToDecimal(dr["Price"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }
                else
                {
                    TempData["Error"] = "Lab Test not found.";
                    return RedirectToAction("ManageLabTest");
                }
            }

            return View("~/Views/Admin/LabTest/EditLabTest.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLabTest(LabTestModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/LabTest/EditLabTest.cshtml", model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM tbl_LabTest WHERE TestName=@TestName AND LabTestId<>@LabTestId AND IsDeleted=0", con);
                    checkCmd.Parameters.AddWithValue("@TestName", model.TestName);
                    checkCmd.Parameters.AddWithValue("@LabTestId", model.LabTestId);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "Another Lab Test with this name already exists.";
                        return View("~/Views/Admin/LabTest/EditLabTest.cshtml", model);
                    }

                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_LabTest
                        SET TestName = @TestName,
                            TestCategory = @TestCategory,
                            Description = @Description,
                            SampleType = @SampleType,
                            PreparationInstructions = @PreparationInstructions,
                            ReportTime = @ReportTime,
                            Price = @Price,
                            IsActive = @IsActive,
                            UpdatedDate = GETDATE()
                        WHERE LabTestId = @LabTestId", con);

                    cmd.Parameters.AddWithValue("@TestName", model.TestName);
                    cmd.Parameters.AddWithValue("@TestCategory", (object)model.TestCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SampleType", (object)model.SampleType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreparationInstructions", (object)model.PreparationInstructions ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportTime", (object)model.ReportTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Price", model.Price);
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                    cmd.Parameters.AddWithValue("@LabTestId", model.LabTestId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Lab Test updated successfully.";
                    return RedirectToAction("ManageLabTest");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Admin/LabTest/EditLabTest.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ViewLabTest(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabTestModel model = new LabTestModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_LabTest WHERE LabTestId = @LabTestId AND IsDeleted = 0", con);
                cmd.Parameters.AddWithValue("@LabTestId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.TestName = dr["TestName"].ToString();
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "";
                    model.SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "";
                    model.PreparationInstructions = dr["PreparationInstructions"] != DBNull.Value ? dr["PreparationInstructions"].ToString() : "";
                    model.ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "";
                    model.Price = Convert.ToDecimal(dr["Price"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    model.UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null;
                }
                else
                {
                    TempData["Error"] = "Lab Test not found.";
                    return RedirectToAction("ManageLabTest");
                }
            }

            return View("~/Views/Admin/LabTest/ViewLabTest.cshtml", model);
        }

        [HttpGet]
        public IActionResult DeleteLabTest(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE tbl_LabTest SET IsDeleted = 1, UpdatedDate = GETDATE() WHERE LabTestId = @LabTestId", con);
                    cmd.Parameters.AddWithValue("@LabTestId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Test deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Test not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabTest");
        }

        [HttpGet]
        public IActionResult ChangeLabTestStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabTestModel model = new LabTestModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT LabTestId, TestName, IsActive FROM tbl_LabTest WHERE LabTestId=@LabTestId AND IsDeleted=0", con);
                cmd.Parameters.AddWithValue("@LabTestId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.TestName = dr["TestName"].ToString();
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }
                else
                {
                    TempData["Error"] = "Lab Test not found.";
                    return RedirectToAction("ManageLabTest");
                }
            }

            return View("~/Views/Admin/LabTest/ChangeLabTestStatus.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLabTestStatus(long id, bool isActive)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE tbl_LabTest SET IsActive = @IsActive, UpdatedDate = GETDATE() WHERE LabTestId = @LabTestId AND IsDeleted = 0", con);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);
                    cmd.Parameters.AddWithValue("@LabTestId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Test status updated successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Test not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabTest");
        }


        [HttpGet]
        public IActionResult ManageLabBooking(string search = "", string status = "", string paymentStatus = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<LabBookingModel> list = new List<LabBookingModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT B.*, T.TestName, T.TestCategory, T.SampleType, T.ReportTime, T.Price AS TestPrice, C.FullName AS CustomerName
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON B.CustomerId = C.CustomerId
                    WHERE B.IsDeleted = 0
                    AND (@Search = '' OR B.BookingNo LIKE '%' + @Search + '%' OR B.PatientName LIKE '%' + @Search + '%' OR B.MobileNo LIKE '%' + @Search + '%')
                    AND (@Status = '' OR B.TestStatus = @Status)
                    AND (@PaymentStatus = '' OR B.PaymentStatus = @PaymentStatus)
                    ORDER BY B.LabBookingId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@Status", status ?? "");
                cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus ?? "");

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabBookingModel model = new LabBookingModel
                    {
                        LabBookingId = Convert.ToInt64(dr["LabBookingId"]),
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        BookingNo = dr["BookingNo"].ToString(),
                        BookingDate = Convert.ToDateTime(dr["BookingDate"]),
                        PatientName = dr["PatientName"].ToString(),
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        Age = dr["Age"] != DBNull.Value ? Convert.ToInt32(dr["Age"]) : null,
                        Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "",
                        PreferredDate = dr["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(dr["PreferredDate"]) : null,
                        PreferredTime = dr["PreferredTime"] != DBNull.Value ? dr["PreferredTime"].ToString() : "",
                        HealthConcern = dr["HealthConcern"] != DBNull.Value ? dr["HealthConcern"].ToString() : "",
                        TestStatus = dr["TestStatus"].ToString(),
                        PaymentStatus = dr["PaymentStatus"].ToString(),
                        Amount = Convert.ToDecimal(dr["Amount"]),
                        AdminRemark = dr["AdminRemark"] != DBNull.Value ? dr["AdminRemark"].ToString() : "",
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        IsDeleted = Convert.ToBoolean(dr["IsDeleted"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null,
                        TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "",
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "",
                        SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "",
                        ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "",
                        TestPrice = dr["TestPrice"] != DBNull.Value ? Convert.ToDecimal(dr["TestPrice"]) : 0,
                        CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : ""
                    };
                    list.Add(model);
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.PaymentStatus = paymentStatus;
            return View("~/Views/Admin/LabBooking/ManageLabBooking.cshtml", list);
        }

        [HttpGet]
        public IActionResult ViewLabBooking(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabBookingModel model = new LabBookingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT B.*, T.TestName, T.TestCategory, T.SampleType, T.ReportTime, T.Price AS TestPrice, C.FullName AS CustomerName
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON B.CustomerId = C.CustomerId
                    WHERE B.LabBookingId = @LabBookingId AND B.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabBookingId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.BookingNo = dr["BookingNo"].ToString();
                    model.BookingDate = Convert.ToDateTime(dr["BookingDate"]);
                    model.PatientName = dr["PatientName"].ToString();
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.Age = dr["Age"] != DBNull.Value ? Convert.ToInt32(dr["Age"]) : null;
                    model.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    model.PreferredDate = dr["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(dr["PreferredDate"]) : null;
                    model.PreferredTime = dr["PreferredTime"] != DBNull.Value ? dr["PreferredTime"].ToString() : "";
                    model.HealthConcern = dr["HealthConcern"] != DBNull.Value ? dr["HealthConcern"].ToString() : "";
                    model.TestStatus = dr["TestStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.Amount = Convert.ToDecimal(dr["Amount"]);
                    model.AdminRemark = dr["AdminRemark"] != DBNull.Value ? dr["AdminRemark"].ToString() : "";
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    model.UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null;
                    model.TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "";
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "";
                    model.ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "";
                    model.TestPrice = dr["TestPrice"] != DBNull.Value ? Convert.ToDecimal(dr["TestPrice"]) : 0;
                    model.CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "";
                }
                else
                {
                    TempData["Error"] = "Lab Booking not found.";
                    return RedirectToAction("ManageLabBooking");
                }
            }

            return View("~/Views/Admin/LabBooking/ViewLabBooking.cshtml", model);
        }

        [HttpGet]
        public IActionResult ChangeLabBookingStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabBookingModel model = new LabBookingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT B.*, T.TestName, C.FullName AS CustomerName
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON B.CustomerId = C.CustomerId
                    WHERE B.LabBookingId = @LabBookingId AND B.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabBookingId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.BookingNo = dr["BookingNo"].ToString();
                    model.PatientName = dr["PatientName"].ToString();
                    model.TestStatus = dr["TestStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.AdminRemark = dr["AdminRemark"] != DBNull.Value ? dr["AdminRemark"].ToString() : "";
                    model.TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "";
                    model.CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "";
                }
                else
                {
                    TempData["Error"] = "Lab Booking not found.";
                    return RedirectToAction("ManageLabBooking");
                }
            }

            return View("~/Views/Admin/LabBooking/ChangeLabBookingStatus.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLabBookingStatus(long id, string testStatus, string paymentStatus, string adminRemark)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_LabBooking
                        SET TestStatus = @TestStatus,
                            PaymentStatus = @PaymentStatus,
                            AdminRemark = @AdminRemark,
                            UpdatedDate = GETDATE()
                        WHERE LabBookingId = @LabBookingId AND IsDeleted = 0", con);

                    cmd.Parameters.AddWithValue("@TestStatus", testStatus ?? "Pending");
                    cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus ?? "Pending");
                    cmd.Parameters.AddWithValue("@AdminRemark", (object)adminRemark ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LabBookingId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Booking status updated successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Booking not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabBooking");
        }

        [HttpGet]
        public IActionResult DeleteLabBooking(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE tbl_LabBooking SET IsDeleted = 1, UpdatedDate = GETDATE() WHERE LabBookingId = @LabBookingId", con);
                    cmd.Parameters.AddWithValue("@LabBookingId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Booking deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Booking not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabBooking");
        }

        // =========================================================
        // LAB REPORT MODULE
        // =========================================================

        [HttpGet]
        public IActionResult ManageLabReport(string search = "", string status = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<LabReportModel> list = new List<LabReportModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, B.MobileNo, B.Email, B.TestStatus, B.Amount, B.BookingDate,
                           T.TestName, T.TestCategory, C.FullName AS CustomerName
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON R.CustomerId = C.CustomerId
                    WHERE R.IsDeleted = 0
                    AND (@Search = '' OR R.ReportNo LIKE '%' + @Search + '%' OR B.BookingNo LIKE '%' + @Search + '%' OR B.PatientName LIKE '%' + @Search + '%' OR R.ReportTitle LIKE '%' + @Search + '%')
                    AND (@Status = '' OR R.ReportStatus = @Status)
                    ORDER BY R.LabReportId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabReportModel model = new LabReportModel
                    {
                        LabReportId = Convert.ToInt64(dr["LabReportId"]),
                        LabBookingId = Convert.ToInt64(dr["LabBookingId"]),
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        ReportNo = dr["ReportNo"].ToString(),
                        ReportDate = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"]) : null,
                        ReportFile = dr["ReportFile"] != DBNull.Value ? dr["ReportFile"].ToString() : "",
                        ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "",
                        TechnicianName = dr["TechnicianName"] != DBNull.Value ? dr["TechnicianName"].ToString() : "",
                        ReportStatus = dr["ReportStatus"].ToString(),
                        ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "",
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        IsDeleted = Convert.ToBoolean(dr["IsDeleted"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null,
                        BookingNo = dr["BookingNo"] != DBNull.Value ? dr["BookingNo"].ToString() : "",
                        TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "",
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "",
                        CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "",
                        PatientName = dr["PatientName"] != DBNull.Value ? dr["PatientName"].ToString() : "",
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        TestStatus = dr["TestStatus"] != DBNull.Value ? dr["TestStatus"].ToString() : "",
                        Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
                        BookingDate = dr["BookingDate"] != DBNull.Value ? Convert.ToDateTime(dr["BookingDate"]) : null
                    };
                    list.Add(model);
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            return View("~/Views/Admin/LabReport/ManageLabReport.cshtml", list);
        }

        [HttpGet]
        public IActionResult UploadLabReport(long? bookingId = null)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabReportModel model = new LabReportModel();
            List<SelectListItem> bookingList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT B.LabBookingId, B.BookingNo, B.PatientName, B.LabTestId, B.CustomerId, T.TestName
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    WHERE B.IsDeleted = 0 ORDER BY B.LabBookingId DESC", con);

                SqlDataReader dr = cmd.ExecuteReader();
                bookingList.Add(new SelectListItem { Text = "-- Select Booking --", Value = "" });
                while (dr.Read())
                {
                    string bId = dr["LabBookingId"].ToString();
                    bookingList.Add(new SelectListItem
                    {
                        Text = $"{dr["BookingNo"]} - {dr["PatientName"]} ({dr["TestName"]})",
                        Value = bId,
                        Selected = (bookingId.HasValue && bookingId.Value.ToString() == bId)
                    });
                }
                dr.Close();

                if (bookingId.HasValue && bookingId.Value > 0)
                {
                    SqlCommand bCmd = new SqlCommand(@"
                        SELECT B.LabBookingId, B.LabTestId, B.CustomerId, B.BookingNo, B.PatientName, T.TestName
                        FROM tbl_LabBooking B
                        INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                        WHERE B.LabBookingId = @LabBookingId AND B.IsDeleted = 0", con);
                    bCmd.Parameters.AddWithValue("@LabBookingId", bookingId.Value);
                    SqlDataReader bDr = bCmd.ExecuteReader();
                    if (bDr.Read())
                    {
                        model.LabBookingId = Convert.ToInt64(bDr["LabBookingId"]);
                        model.LabTestId = Convert.ToInt64(bDr["LabTestId"]);
                        model.CustomerId = Convert.ToInt64(bDr["CustomerId"]);
                        model.BookingNo = bDr["BookingNo"].ToString();
                        model.PatientName = bDr["PatientName"].ToString();
                        model.TestName = bDr["TestName"].ToString();
                        model.ReportTitle = $"{bDr["TestName"]} Report for {bDr["PatientName"]}";
                    }
                    bDr.Close();
                }
            }

            ViewBag.BookingList = bookingList;
            return View("~/Views/Admin/LabReport/UploadLabReport.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadLabReport(LabReportModel model, IFormFile? ReportFile)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (model.LabBookingId <= 0)
            {
                ModelState.AddModelError("LabBookingId", "Please select a Lab Booking.");
            }

            if (ReportFile == null || ReportFile.Length == 0)
            {
                ModelState.AddModelError("ReportFile", "Please upload a valid PDF report file.");
            }
            else if (!Path.GetExtension(ReportFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("ReportFile", "Only PDF files are allowed.");
            }

            if (!ModelState.IsValid)
            {
                List<SelectListItem> bookingList = new List<SelectListItem>();
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(@"
                        SELECT B.LabBookingId, B.BookingNo, B.PatientName, T.TestName
                        FROM tbl_LabBooking B
                        INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                        WHERE B.IsDeleted = 0 ORDER BY B.LabBookingId DESC", con);
                    SqlDataReader dr = cmd.ExecuteReader();
                    bookingList.Add(new SelectListItem { Text = "-- Select Booking --", Value = "" });
                    while (dr.Read())
                    {
                        bookingList.Add(new SelectListItem
                        {
                            Text = $"{dr["BookingNo"]} - {dr["PatientName"]} ({dr["TestName"]})",
                            Value = dr["LabBookingId"].ToString(),
                            Selected = (model.LabBookingId.ToString() == dr["LabBookingId"].ToString())
                        });
                    }
                }
                ViewBag.BookingList = bookingList;
                return View("~/Views/Admin/LabReport/UploadLabReport.cshtml", model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Get LabTestId and CustomerId from booking if not provided
                    SqlCommand bCmd = new SqlCommand("SELECT LabTestId, CustomerId FROM tbl_LabBooking WHERE LabBookingId=@LabBookingId", con);
                    bCmd.Parameters.AddWithValue("@LabBookingId", model.LabBookingId);
                    SqlDataReader bDr = bCmd.ExecuteReader();
                    if (bDr.Read())
                    {
                        model.LabTestId = Convert.ToInt64(bDr["LabTestId"]);
                        model.CustomerId = Convert.ToInt64(bDr["CustomerId"]);
                    }
                    bDr.Close();

                    // Save File
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LabReports");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = "Report_" + Guid.NewGuid().ToString("N") + Path.GetExtension(ReportFile!.FileName);
                    string filePath = Path.Combine(folderPath, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        ReportFile.CopyTo(stream);
                    }

                    string reportNo = "REP" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO tbl_LabReport (LabBookingId, LabTestId, CustomerId, ReportNo, ReportDate, ReportFile, ReportTitle, TechnicianName, ReportStatus, ReportRemark, IsActive, IsDeleted, CreatedDate)
                        VALUES (@LabBookingId, @LabTestId, @CustomerId, @ReportNo, GETDATE(), @ReportFile, @ReportTitle, @TechnicianName, @ReportStatus, @ReportRemark, 1, 0, GETDATE())", con);

                    cmd.Parameters.AddWithValue("@LabBookingId", model.LabBookingId);
                    cmd.Parameters.AddWithValue("@LabTestId", model.LabTestId);
                    cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                    cmd.Parameters.AddWithValue("@ReportNo", reportNo);
                    cmd.Parameters.AddWithValue("@ReportFile", fileName);
                    cmd.Parameters.AddWithValue("@ReportTitle", (object)model.ReportTitle ?? "Lab Test Report");
                    cmd.Parameters.AddWithValue("@TechnicianName", (object)model.TechnicianName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportStatus", string.IsNullOrEmpty(model.ReportStatus) ? "Completed" : model.ReportStatus);
                    cmd.Parameters.AddWithValue("@ReportRemark", (object)model.ReportRemark ?? DBNull.Value);

                    cmd.ExecuteNonQuery();

                    // Update corresponding booking status to Completed
                    SqlCommand uCmd = new SqlCommand("UPDATE tbl_LabBooking SET TestStatus = 'Completed', UpdatedDate = GETDATE() WHERE LabBookingId = @LabBookingId", con);
                    uCmd.Parameters.AddWithValue("@LabBookingId", model.LabBookingId);
                    uCmd.ExecuteNonQuery();

                    TempData["Success"] = "Lab Report uploaded successfully.";
                    return RedirectToAction("ManageLabReport");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Admin/LabReport/UploadLabReport.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ViewLabReport(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabReportModel model = new LabReportModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, B.MobileNo, B.Email, B.TestStatus, B.Amount, B.BookingDate,
                           T.TestName, T.TestCategory, C.FullName AS CustomerName
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON R.CustomerId = C.CustomerId
                    WHERE R.LabReportId = @LabReportId AND R.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabReportId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabReportId = Convert.ToInt64(dr["LabReportId"]);
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.ReportNo = dr["ReportNo"].ToString();
                    model.ReportDate = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"]) : null;
                    model.ReportFile = dr["ReportFile"] != DBNull.Value ? dr["ReportFile"].ToString() : "";
                    model.ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "";
                    model.TechnicianName = dr["TechnicianName"] != DBNull.Value ? dr["TechnicianName"].ToString() : "";
                    model.ReportStatus = dr["ReportStatus"].ToString();
                    model.ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "";
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    model.UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null;
                    model.BookingNo = dr["BookingNo"] != DBNull.Value ? dr["BookingNo"].ToString() : "";
                    model.TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "";
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "";
                    model.PatientName = dr["PatientName"] != DBNull.Value ? dr["PatientName"].ToString() : "";
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.TestStatus = dr["TestStatus"] != DBNull.Value ? dr["TestStatus"].ToString() : "";
                    model.Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0;
                    model.BookingDate = dr["BookingDate"] != DBNull.Value ? Convert.ToDateTime(dr["BookingDate"]) : null;
                }
                else
                {
                    TempData["Error"] = "Lab Report not found.";
                    return RedirectToAction("ManageLabReport");
                }
            }

            return View("~/Views/Admin/LabReport/ViewLabReport.cshtml", model);
        }

        [HttpGet]
        public IActionResult EditLabReport(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabReportModel model = new LabReportModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, T.TestName
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    WHERE R.LabReportId = @LabReportId AND R.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabReportId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabReportId = Convert.ToInt64(dr["LabReportId"]);
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.ReportNo = dr["ReportNo"].ToString();
                    model.ReportDate = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"]) : null;
                    model.ReportFile = dr["ReportFile"] != DBNull.Value ? dr["ReportFile"].ToString() : "";
                    model.ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "";
                    model.TechnicianName = dr["TechnicianName"] != DBNull.Value ? dr["TechnicianName"].ToString() : "";
                    model.ReportStatus = dr["ReportStatus"].ToString();
                    model.ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "";
                    model.BookingNo = dr["BookingNo"].ToString();
                    model.PatientName = dr["PatientName"].ToString();
                    model.TestName = dr["TestName"].ToString();
                }
                else
                {
                    TempData["Error"] = "Lab Report not found.";
                    return RedirectToAction("ManageLabReport");
                }
            }

            return View("~/Views/Admin/LabReport/EditLabReport.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLabReport(LabReportModel model, IFormFile? ReportFile)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string oldFile = "";
                    SqlCommand oldCmd = new SqlCommand("SELECT ReportFile FROM tbl_LabReport WHERE LabReportId=@LabReportId", con);
                    oldCmd.Parameters.AddWithValue("@LabReportId", model.LabReportId);
                    object obj = oldCmd.ExecuteScalar();
                    if (obj != null)
                    {
                        oldFile = obj.ToString();
                    }

                    string fileName = oldFile;

                    if (ReportFile != null && ReportFile.Length > 0)
                    {
                        if (!Path.GetExtension(ReportFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            ViewBag.Error = "Only PDF files are allowed.";
                            return View("~/Views/Admin/LabReport/EditLabReport.cshtml", model);
                        }

                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LabReports");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        if (!string.IsNullOrEmpty(oldFile))
                        {
                            string oldPath = Path.Combine(folderPath, oldFile);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        fileName = "Report_" + Guid.NewGuid().ToString("N") + Path.GetExtension(ReportFile.FileName);
                        string newPath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(newPath, FileMode.Create))
                        {
                            ReportFile.CopyTo(stream);
                        }
                    }

                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_LabReport
                        SET ReportTitle = @ReportTitle,
                            TechnicianName = @TechnicianName,
                            ReportStatus = @ReportStatus,
                            ReportRemark = @ReportRemark,
                            ReportFile = @ReportFile,
                            UpdatedDate = GETDATE()
                        WHERE LabReportId = @LabReportId AND IsDeleted = 0", con);

                    cmd.Parameters.AddWithValue("@ReportTitle", (object)model.ReportTitle ?? "Lab Test Report");
                    cmd.Parameters.AddWithValue("@TechnicianName", (object)model.TechnicianName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportStatus", (object)model.ReportStatus ?? "Completed");
                    cmd.Parameters.AddWithValue("@ReportRemark", (object)model.ReportRemark ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportFile", fileName);
                    cmd.Parameters.AddWithValue("@LabReportId", model.LabReportId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Lab Report updated successfully.";
                    return RedirectToAction("ManageLabReport");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Admin/LabReport/EditLabReport.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult DeleteLabReport(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE tbl_LabReport SET IsDeleted = 1, UpdatedDate = GETDATE() WHERE LabReportId = @LabReportId", con);
                    cmd.Parameters.AddWithValue("@LabReportId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Report deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Report not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabReport");
        }

        [HttpGet]
        public IActionResult ChangeLabReportStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            LabReportModel model = new LabReportModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, T.TestName
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    WHERE R.LabReportId = @LabReportId AND R.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabReportId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabReportId = Convert.ToInt64(dr["LabReportId"]);
                    model.ReportNo = dr["ReportNo"].ToString();
                    model.ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "";
                    model.ReportStatus = dr["ReportStatus"].ToString();
                    model.ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "";
                    model.BookingNo = dr["BookingNo"].ToString();
                    model.PatientName = dr["PatientName"].ToString();
                    model.TestName = dr["TestName"].ToString();
                }
                else
                {
                    TempData["Error"] = "Lab Report not found.";
                    return RedirectToAction("ManageLabReport");
                }
            }

            return View("~/Views/Admin/LabReport/ChangeLabReportStatus.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLabReportStatus(long id, string reportStatus, string reportRemark)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_LabReport
                        SET ReportStatus = @ReportStatus,
                            ReportRemark = @ReportRemark,
                            UpdatedDate = GETDATE()
                        WHERE LabReportId = @LabReportId AND IsDeleted = 0", con);

                    cmd.Parameters.AddWithValue("@ReportStatus", reportStatus ?? "Completed");
                    cmd.Parameters.AddWithValue("@ReportRemark", (object)reportRemark ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LabReportId", id);

                    con.Open();
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Lab Report status updated successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Lab Report not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageLabReport");
        }

        // ===========================
        // Logout
        // ===========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
