from pydantic import BaseModel, ConfigDict
from typing import Optional, List, Any
from enum import IntEnum

class UserRole(IntEnum):
    Admin = 0
    Teacher = 1
    Student = 2

class SubjectBase(BaseModel):
    Name: str

class SubjectCreate(SubjectBase):
    pass

class Subject(SubjectBase):
    Id: int
    model_config = ConfigDict(from_attributes=True)

class ClassroomBase(BaseModel):
    Number: str
    Capacity: Optional[int] = None
    Type: Optional[str] = None

class ClassroomCreate(ClassroomBase):
    pass

class Classroom(ClassroomBase):
    Id: int
    model_config = ConfigDict(from_attributes=True)

class TeacherBase(BaseModel):
    FullName: str
    SubjectId: Optional[int] = None
    ClassroomId: Optional[int] = None

class TeacherCreate(TeacherBase):
    pass

class Teacher(TeacherBase):
    Id: int
    Subject: Optional[Any] = None
    Classroom: Optional[Any] = None
    model_config = ConfigDict(from_attributes=True)

class AcademicClassBase(BaseModel):
    Name: str
    StudentCount: int
    Shift: int
    CuratorTeacherId: Optional[int] = None

class AcademicClassCreate(AcademicClassBase):
    pass

class AcademicClass(AcademicClassBase):
    Id: int
    CuratorTeacher: Optional[Any] = None
    model_config = ConfigDict(from_attributes=True)

class WorkloadBase(BaseModel):
    TeacherId: int
    SubjectId: int
    AcademicClassId: int
    HoursPerWeek: int

class WorkloadCreate(WorkloadBase):
    pass

class Workload(WorkloadBase):
    Id: int
    Teacher: Optional[Any] = None
    Subject: Optional[Any] = None
    AcademicClass: Optional[Any] = None
    model_config = ConfigDict(from_attributes=True)

class LessonBase(BaseModel):
    DayOfWeek: int
    LessonIndex: int
    TeacherId: int
    SubjectId: int
    AcademicClassId: int
    ClassroomId: Optional[int] = None

class LessonCreate(LessonBase):
    pass

class Lesson(LessonBase):
    Id: int
    Teacher: Optional[Any] = None
    Subject: Optional[Any] = None
    AcademicClass: Optional[Any] = None
    Classroom: Optional[Any] = None
    model_config = ConfigDict(from_attributes=True)

class UserBase(BaseModel):
    Username: str
    FullName: str
    Role: UserRole = UserRole.Student
    TeacherId: Optional[int] = None
    AcademicClassId: Optional[int] = None

class UserCreate(UserBase):
    Password: str

class User(UserBase):
    Id: int
    Teacher: Optional[Any] = None
    AcademicClass: Optional[Any] = None
    model_config = ConfigDict(from_attributes=True)

class UserLogin(BaseModel):
    Username: str
    Password: str
