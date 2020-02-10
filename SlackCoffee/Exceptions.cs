using System;

namespace SlackCoffee
{
    public class BadRequestException : Exception
    {
        public virtual string ResponseMsg => Message;

        public BadRequestException() { }

        public BadRequestException(string msg) : base (msg) { }
    }


    public class CommandNotFoundException : BadRequestException
    {
        public override string ResponseMsg => "없는 명령어 입니다.";
    }

    public class OnlyForManagerException : BadRequestException
    {
        public override string ResponseMsg => "운영자 전용 명령어 입니다.";
    }

    public class NotWellFormedException : BadRequestException
    {
        public override string ResponseMsg => "잘못된 형식 입니다.";
    }

    public class NoOneToPickException : BadRequestException
    {
        public override string ResponseMsg => "추첨할 인원이 없습니다.";
    }

    public class UserNotFoundException : BadRequestException
    {
        public override string ResponseMsg => "존재하지 않는 사용자 입니다.";
    }

    public class MenuNotFoundException : BadRequestException
    {
        public override string ResponseMsg => "존재하지 않는 메뉴 입니다.";
    }
}
