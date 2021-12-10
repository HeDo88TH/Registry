FROM ubuntu:focal as ddb-builder

LABEL Author="Luca Di Leo <ldileo@digipa.it>"

# First image is the builder for ddb

# Prerequisites
ENV TZ=Europe/Rome
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
RUN apt update && apt install -y --fix-missing --no-install-recommends build-essential software-properties-common
RUN add-apt-repository -y ppa:ubuntugis/ubuntugis-unstable
RUN apt install -y --fix-missing --no-install-recommends ca-certificates cmake git checkinstall sqlite3 spatialite-bin libgeos-dev libgdal-dev g++-10 gcc-10 pdal libpdal-dev libzip-dev

# Build DroneDB
RUN git clone --recurse-submodules https://github.com/DroneDB/DroneDB.git
RUN cd DroneDB && mkdir build && cd build && \
    cmake .. && \
    make -j $(nproc)
RUN cd /DroneDB/build && checkinstall --install=no --pkgname DroneDB --default

# -> Output: /DroneDB/build/dronedb_20211209-1_amd64.deb and /DroneDB/build/libddb.so

# Second image is the builder for Registry and Hub

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal as web-builder

COPY . /Registry

# Install NodeJS
RUN apt update && apt install -y --fix-missing sudo gpg-agent curl lsb-release
RUN curl -sL https://deb.nodesource.com/setup_14.x | sudo -E bash -
RUN apt update && apt install -y --fix-missing nodejs

# Compile client app
RUN npm install -g webpack@4 webpack-cli
RUN cd /Registry/Registry.Web/ClientApp && npm install && webpack --mode=production

RUN cd /Registry/Registry.Web && dotnet dev-certs https && dotnet publish --configuration Release /p:PublishProfile=FolderProfile
# published files are in /Registry/Registry.Web/bin/Release/net5.0/linux-x64

# Third and final image is the runner that will get all the build files from the previous images
FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal as runner
ENV TZ=Europe/Rome
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

RUN apt update && apt install -y --fix-missing --no-install-recommends gnupg

# Add gis ppa
RUN echo "deb http://ppa.launchpad.net/ubuntugis/ubuntugis-unstable/ubuntu focal main" > /etc/apt/sources.list.d/ubuntugis.list 
RUN echo "deb-src http://ppa.launchpad.net/ubuntugis/ubuntugis-unstable/ubuntu focal main" >> /etc/apt/sources.list.d/ubuntugis.list
RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 6B827C12C2D425E227EDCA75089EBE08314DF160

# Install DroneDB dependencies
RUN apt install -y --fix-missing --no-install-recommends libspatialite7 libsqlite3-0 libexiv2-27 libgdal29 libcurl4 libzip5 libstdc++6 libgcc-s1 libc6 libpdal-base12

# Install DroneDB from deb package and set library path
COPY --from=ddb-builder /DroneDB/build/ /DroneDB
RUN cd /DroneDB && dpkg -i *.deb
RUN cp /DroneDB/libddb.so /usr/lib/libddb.so
ENV LD_LIBRARY_PATH="/usr/local/lib:${LD_LIBRARY_PATH}"

# Copy compiled client app in the appropriate folder
RUN mkdir -p /Registry/Registry.Web/bin/Release/net5.0/linux-x64/ClientApp/build
RUN cp -r /Registry/Registry.Web/ClientApp/build /Registry/Registry.Web/bin/Release/net5.0/linux-x64/ClientApp

EXPOSE 5000/tcp
EXPOSE 5001/tcp

WORKDIR /Registry/Registry.Web/bin/Release/net5.0/linux-x64

# Run registry
ENTRYPOINT dotnet Registry.Web.dll --urls="http://0.0.0.0:5000;https://0.0.0.0:5001"

# RUN apt install libspatialite7 libsqlite3-0 libexiv2-27 libgdal29 libcurl4 libzip5 libstdc++6 libgcc-s1 libc6 libpdal-base12
#[libddb.so]
#[libspatialite.so.7] libspatialite7
#[libsqlite3.so.0] libsqlite3-0
#[libexiv2.so.27] libexiv2-27
#[libgdal.so.29] libgdal29
#[libcurl-gnutls.so.4] libcurl4 (libcurl4-gnutls-dev)
#[libzip.so.5] libzip5
#[libstdc++.so.6] libstdc++6
#[libgcc_s.so.1] libgcc-s1
#[libc.so.6] libc6

# g++-10 gcc-10
#RUN update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-10 1000 --slave /usr/bin/g++ g++ /usr/bin/g++-10
