using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        string cs = @"Data Source=DESKTOP-J2OEC9R\SQLEXPRESS02;Initial Catalog=ClinicDB;Integrated Security=True;TrustServerCertificate=True";

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
