using CompetenceProfilingDomain.Contracts.Infrastructure;
using CompetenceProfilingDomain.Contracts.ModelsDatabase;
using CompetenceProfilingDomain.Definitions;
using CompetenceProfilingDomain.Extensions;
using Microsoft.Extensions.Caching.Distributed;

namespace CompetenceProfilingDomain.Domain;

public class CardCollection
{
    private readonly IRepository _repository;
    private readonly IAssignmentRubricDao _assignmentRubricDao;
    private readonly IAssignmentRubricCriteriaRatingDao _assignmentRubricCriteriaRatingDao;
    private readonly IDistributedCache _distributedCache;

    public CardCollection(IRepository repository, IAssignmentRubricDao assignmentRubricDao, IAssignmentRubricCriteriaRatingDao assignmentRubricCriteriaRatingDao, IDistributedCache distributedCache)
    {
        _repository = repository;
        _assignmentRubricDao = assignmentRubricDao;
        _assignmentRubricCriteriaRatingDao = assignmentRubricCriteriaRatingDao;
        _distributedCache = distributedCache;
        
        
    }

    // public List<Card> GetAllCards(int courseId, int assignmentId, int userId)
    // {
    //     RubricAssociationId(assignmentId); // check rubric number
    //     var rubricCriteria = RubricCriteria(assignmentId);
    //     
    //     var submissionsCanvas = _assignmentRubricCriteriaRatingDao.SubmissionsByCourseAndAssignmentAndUser(courseId, assignmentId, userId);
    //     var submissionsStudent = _repository.Query<StudentAdviceDto>().Where(w => w.CourseId == courseId & w.UserId == userId).ToList();
    //     //var studKpi = _databaseContext.StudentKpi.Where(w => w.UserId == userId && w.CourseId != courseId).ToList();
    //     var studKpi = _repository.Query<StudentKpiDto>().Where(w => w.UserId == userId && w.CourseId != courseId).ToList();
    //     
    //     var ret = new List<Card>();
    //     foreach (var criteria in rubricCriteria)
    //     {
    //         var subCan = submissionsCanvas?.RubricAssessment.FirstOrDefault(f => f._id == criteria.Id);
    //         var subStud = submissionsStudent.FirstOrDefault(f=>f.CriteriaId== criteria.Id);
    //
    //         var card = new Card(criteria.Id, criteria.OutcomeCanvasDto.Id, criteria.Description, criteria.LongDescription);
    //         
    //         if(subStud!= null) // stud advises geel
    //         {
    //             if(subStud.Point== (int)PointScale.StudentAdvice)
    //                 card.Points = subStud.Point;
    //         }
    //
    //         if (subCan?.Points is "3" or "4" or "5")  // canvas scale
    //         {
    //             card.Points = (int)PointScale.Mastered;
    //         }
    //
    //         card.CourseHistory = studKpi.Where(w 
    //             => w.OutcomeId == criteria.OutcomeCanvasDto.Id && w.Point == (int)PointScale.Mastered
    //             || w.CriteriaId == criteria.Id && w.Point==(int)PointScale.Mastered
    //             
    //             ).Select(s=>s.CourseId).ToList();
    //         
    //         ret.Add(card);
    //     }
    //     return ret;
    // }

    // private List<CriterionCanvasDto> RubricCriteria(int assignmentId)
    // {
    //     var buff = _distributedCache.GetDistributedCache<List<CriterionCanvasDto>>("ListCriterion" + assignmentId);
    //     if (buff != null)
    //     {
    //         return buff;
    //     }
    //     
    //     var data = _assignmentRubricDao.RubricAssignment(assignmentId);
    //     var rubricCriteria = data.Assignment?.RubricCanvasDto.Criteria;
    //     if (rubricCriteria == null) throw new Exception("Rubric Criteria not found");
    //     
    //     _distributedCache.SetDistributedCache("ListCriterion" + assignmentId,rubricCriteria,15);
    //     return rubricCriteria;
    // }


    public void UpdateCards(int courseId,int userId,int assignmentId, IReadOnlyList<PointViewModel> points, bool isTeacherRole = false)
    {
        if (isTeacherRole) 
            UpdateTaskRubricAndSubmissions(courseId, userId, assignmentId, points);
        UpdateStudentAdvicesDatabase(courseId, userId, points);
    }

    private void UpdateTaskRubricAndSubmissions(int courseId, int userId, int assignmentId, IReadOnlyList<PointViewModel> points)
    {
        var tasks = new Task[2];
        tasks[0] = Task.Run(() => CreateAndUpdateRubricSubmissions(courseId, userId, assignmentId, points));
        tasks[1] = Task.Run(() => UpdateRubricSubmissionsDatabase(courseId, userId, points));

        try
        {
            Task.WaitAll(tasks);
            Console.WriteLine("Task Done");
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.Flatten().InnerExceptions)
                throw new Exception(ex.Message);
        }
    }

    private void UpdateRubricSubmissionsDatabase(int courseId, int userId, IReadOnlyList<PointViewModel> points)
    {
        Console.WriteLine("Start UpdateRubricSubmissionsDatabase");
        //var t = RubricCriteria(assignmentId);
        
        var studOutcomes = _repository.Query<StudentKpiDto>().Where(f => f.UserId == userId && f.CourseId == courseId).ToList();
        foreach (var point in points)
        {
            if(point.Point == (int)PointScale.StudentAdvice) continue;
            
            if(studOutcomes.Any(f=>f.OutcomeId == point.OutcomeId &&  f.Point == point.Point) == false)
                UpdateAndInsertOutcomes(courseId, userId, point.OutcomeId,point.CriteriaId,point.Point);
        }
        //_databaseContext.SaveChanges();
        _repository.SaveChanges();
        Console.WriteLine("End UpdateRubricSubmissionsDatabase");
    }

    private void UpdateAndInsertOutcomes(int courseId, int userId, string outcomeId,string criteriaId, int? point)
    {
       
        // var e = _databaseContext.StudentKpi.FirstOrDefault(f =>
        //     f.OutcomeId == outcomeId && f.UserId == userId && f.CourseId == courseId);
        
        var e =  _repository.Query<StudentKpiDto>().FirstOrDefault(f =>
            f.OutcomeId == outcomeId && f.UserId == userId && f.CourseId == courseId);
        if (e != null)
        {
            if (e.Point != point)
            {
                e.Point = point;
                e.LastUpdated = DateTime.Now;
            }
        }
        else
        {
            var newPoint = new StudentKpiDto()
            {
                UserId = userId,
                CourseId = courseId,
                OutcomeId = outcomeId,
                CriteriaId = criteriaId,
                Point = point,
                LastUpdated = DateTime.Now
            };
            //_databaseContext.StudentKpi.Add(newPoint);
            _repository.Add(newPoint);
        }
    }

    private void CreateAndUpdateRubricSubmissions(int courseId, int userId, int assignmentId, IReadOnlyList<PointViewModel> pointViewModels)
    {
        Console.WriteLine("Start CreateAndUpdateRubricSubmissions");
        var listPv = new List<PointViewModel>();
        foreach (var pointViewModel in pointViewModels)
        {
            int? point = null;
            //if (pointViewModel.Point == 1) point = null;
            if (pointViewModel.Point == (int)PointScale.Mastered) point = 3;   // canvas scale rubric 3 = S

            var pv = new PointViewModel()
            {
                CriteriaId = pointViewModel.CriteriaId,
                OutcomeId = pointViewModel.OutcomeId,
                Point = point
            };
            
            listPv.Add(pv);
        }
        
        var criterionIdAndPoint = listPv.ToDictionary(point => point.CriteriaId, point => point.Point);
        
        
        var rubricAssociationId = RubricAssociationId(assignmentId);
       
        
        _assignmentRubricCriteriaRatingDao.CreateAndUpdateRubricSubmissions(
            courseId,
            userId,
            rubricAssociationId,
            criterionIdAndPoint
        );
        Console.WriteLine("End CreateAndUpdateRubricSubmissions");
    }

    private int RubricAssociationId(int assignmentId)
    {
        var buff = _distributedCache.GetDistributedCache<string>("RubricAssociationId" + assignmentId);
        if (buff != null)
        {
            return int.Parse(buff);
        }
        
        var nr = _assignmentRubricDao.RubricAssociationId(assignmentId);
        if (nr == 0) throw new Exception($"No Rubric found for the assignment-> is er een rubric gekoppeld aan de assignment met nummer: {assignmentId}  ");
        
        _distributedCache.SetDistributedCache("RubricAssociationId" + assignmentId,nr.ToString(),15);
        return nr;
    }

    private void UpdateStudentAdvicesDatabase(int courseId, int userId, IReadOnlyList<PointViewModel> points)
    {
        var studentAdvicesByCourse =
            _repository.Query<StudentAdviceDto>().Where(w => w.UserId == userId && w.CourseId == courseId).ToList();

        foreach (var criteriaPoint in points)
        {
            var criteria = studentAdvicesByCourse.FirstOrDefault(f => f.CriteriaId == criteriaPoint.CriteriaId);
            if (criteria != null)
            {
                if (criteriaPoint.Point == (int)PointScale.StudentAdvice)
                {
                    var ent = _repository.Query<StudentAdviceDto>()
                        .First(w => w.UserId == userId && w.CourseId == courseId && w.CriteriaId == criteriaPoint.CriteriaId);
                    ent.Point = (int)PointScale.StudentAdvice;
                }
                else
                {
                    //_databaseContext.StudentAdvices.Remove(criteria);
                    _repository.Remove(criteria);
                }
            }
            else
            {
                if (criteriaPoint.Point == (int)PointScale.StudentAdvice)
                {
                    _repository.Add(new StudentAdviceDto()
                    {
                        CriteriaId = criteriaPoint.CriteriaId,
                        CourseId = courseId,
                        Point = (int)PointScale.StudentAdvice,
                        UserId = userId,
                        OutcomeId = criteriaPoint.OutcomeId
                    });
                }
            }
        }

        _repository.SaveChanges();
    }
}

