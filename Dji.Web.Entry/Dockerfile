# 注意：运行时镜像必须与项目目标框架一致。
# 本项目目标框架为 net10.0，此前这里的 aspnet:6.0 会导致容器启动即
# "You must install or update .NET to run this application"。
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5005

COPY . .

# 设置语言/区域设置环境变量
ENV LANG zh-Hans

# 使用阿里云的镜像源进行更新
RUN sed -i 's/deb.debian.org/mirrors.aliyun.com/' /etc/apt/sources.list

# 更新包管理器并安装free命令
RUN apt-get update && apt-get install -y procps

ENTRYPOINT ["dotnet", "Dji.Web.Entry.dll"]